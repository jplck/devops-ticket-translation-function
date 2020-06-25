import json
import logging
import os

import azure.functions as func
import requests
from azure.devops.connection import Connection
from msrest.authentication import BasicAuthentication

api_version = os.environ["API_VERSION"]
assert api_version is not None

target_language = os.environ["TARGET_LANGUAGE"]
assert target_language is not None

source_field = os.environ["DESCRIPTION_FIELD"]
assert source_field is not None

target_field = os.environ["TRANSLATION_FIELD"]
assert target_field is not None

translator_endpoint = os.environ["TRANSLATION_ENDPOINT"]
assert translator_endpoint is not None

ocp_apim_key = os.environ["ENDPOINT_SECRET"]
assert ocp_apim_key is not None

endpoint_region = os.environ["ENDPOINT_REGION"]
assert endpoint_region is not None

personal_access_token = os.environ["PERSONAL_ACCESS_TOKEN"]
assert personal_access_token is not None

headers = {
    "Content-Type": "application/json",
    "Ocp-Apim-Subscription-Key": ocp_apim_key,
    "charset": "UTF-8",
    "Ocp-Apim-Subscription-Region": endpoint_region,
}

endpoint = (
    f"{translator_endpoint}translate?api-version={api_version}&to={target_language}"
)


def translate(text) -> str:
    payload = json.dumps([{"text": text}])

    response = requests.request("POST", endpoint, headers=headers, data=payload)

    response_text = json.loads(response.text)

    return response_text[0]["translations"][0]["text"]


def update_devops_workitem(text, project_id, work_item_id, organization_url):
    credentials = BasicAuthentication("", personal_access_token)
    connection = Connection(base_url=organization_url, creds=credentials)
    work_item_client = connection.clients.get_work_item_tracking_client()

    work_item_client.update_work_item(
        document=[{"op": "add", "path": f"/fields/{target_field}", "value": text}],
        id=work_item_id,
        project=project_id,
    )
    logging.info(f"devops workitem '{work_item_id}' updated")


def parse_devops_ticket(req_body) -> (str, str, str, str):
    baseUrl = req_body.get("resourceContainers").get("project").get("baseUrl")
    project_id = req_body.get("resourceContainers").get("project").get("id")
    work_item_id = req_body.get("resource").get("workItemId")
    ticket_description = get_ticket_description(req_body)
    return baseUrl, project_id, work_item_id, ticket_description


def get_ticket_description(req_body) -> str:
    """Checks if ticket description has changed or if translated description field is empty. Returns the description string for translation.

    Args:
        req_body (dict): Request JSON body containing ticket updated information (sent from DevOps to our Webhook).

    Returns:
        (str): The description string (or None if no translation necessary).
    """
    logging.info(f"Request: {req_body}")

    try:
        return req_body.get("resource").get("fields").get(source_field).get("newValue")
    except AttributeError:
        logging.info(f"Field '{source_field}' did not change.")

    try:
        if req_body.get("resource").get("revision").get("fields").get(target_field):
            logging.info(f"Field '{target_field}' has already translated description.")
            return None
    except AttributeError:
        logging.info(f"Field '{target_field}' is empty.")

    try:
        return req_body.get("resource").get("revision").get("fields").get(source_field)
    except AttributeError:
        logging.info(f"Field '{source_field}' is also empty.")
        return None


def main(req: func.HttpRequest) -> func.HttpResponse:
    logging.info("Python HTTP trigger function processed a request.")

    req_body = req.get_json()

    baseUrl, project_id, work_item_id, ticket_description = parse_devops_ticket(
        req_body
    )

    if ticket_description is None:
        return func.HttpResponse("Translation is not necessary.")

    translation = translate(ticket_description)

    update_devops_workitem(
        text=translation,
        organization_url=baseUrl,
        project_id=project_id,
        work_item_id=work_item_id,
    )

    return func.HttpResponse(translation)

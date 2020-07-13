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
    "Ocp-Apim-Subscription-Region": endpoint_region,
    "charset": "UTF-8",
}

translator_endpoint += f"translate?api-version={api_version}&to={target_language}"


def translate(text) -> str:
    logging.info(f"Translating: '{text}'.")
    payload = json.dumps([{"text": text}])
    response = requests.request(
        "POST", translator_endpoint, headers=headers, data=payload
    )
    logging.debug(f"Response: '{response.text}'")
    try:
        response_text = json.loads(response.text)[0]["translations"][0]["text"]
    except KeyError as e:
        return e
    return response_text


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
    logging.info("parsing DevOps Ticket...")
    return {
        "baseUrl": req_body["resourceContainers"]["project"]["baseUrl"],
        "project_id": req_body["resourceContainers"]["project"]["id"],
        "work_item_id": req_body["resource"]["workItemId"],
        "ticket_description": get_ticket_description(req_body),
    }


def get_ticket_description(req_body) -> str:
    logging.info(f"Request: {req_body}")

    if changed_description := (
        req_body.get("resource", {})
        .get("fields", {})
        .get(source_field, {})
        .get("newValue", None)
    ):
        logging.info("'Description' changed -> Translating ticket")
        return changed_description

    logging.info("No updated 'Description'")

    if (
        req_body.get("resource", {})
        .get("revision", {})
        .get("fields", {})
        .get(target_field, None)
    ):
        logging.info("'Translated Description' already exists -> aborting translation")
        return None

    if description := (
        req_body.get("resource", {})
        .get("revision", {})
        .get("fields", {})
        .get(source_field, None)
    ):
        logging.info("'Translated Description' missing -> Translating 'Description''")
        return description

    logging.info("'Description' is missing -> aborting translation")
    return None


def main(req: func.HttpRequest) -> func.HttpResponse:
    logging.info("Python HTTP trigger function processed a request.")
    req_body = req.get_json()

    project_info = parse_devops_ticket(req_body)

    if project_info["ticket_description"] is None:
        logging.info("Translation is not necessary.")
        return func.HttpResponse("Translation is not necessary.")

    translation = translate(project_info["ticket_description"])

    if isinstance(translation, KeyError):
        return func.HttpResponse(f"Key Error: {translation}")

    logging.info("Updating ticket")
    update_devops_workitem(
        text=translation,
        organization_url=project_info["baseUrl"],
        project_id=project_info["project_id"],
        work_item_id=project_info["work_item_id"],
    )
    return func.HttpResponse(translation)

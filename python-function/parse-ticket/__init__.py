import logging
import os
import json
import azure.functions as func
import requests

translator_endpoint = os.environ["TRANSLATION_ENDPOINT"]
assert translator_endpoint is not None

ocp_apim_key = os.environ["ENDPOINT_SECRET"]
assert ocp_apim_key is not None

endpoint_region = os.environ["ENDPOINT_REGION"]
assert endpoint_region is not None

headers = {
    "Content-Type": "application/json",
    "Ocp-Apim-Subscription-Key": ocp_apim_key,
    "charset": "UTF-8",
    "Ocp-Apim-Subscription-Region": endpoint_region,
}


def translate(text, source_language=None, target_language="de", api_version="3.0"):
    endpoint = (
        translator_endpoint
        + f"translate?api-version={api_version}&to={target_language}"
    )

    if source_language:
        endpoint += f"&from={source_language}"

    payload = json.dumps([{"text": text}])

    response = requests.request("POST", endpoint, headers=headers, data=payload)

    return response.content


def main(req: func.HttpRequest) -> func.HttpResponse:
    logging.info("Python HTTP trigger function processed a request.")

    req_body = req.get_json()

    logging.info(f"Request: {req_body}")

    message = req_body.get("message")

    translations = translate(message.get("text"))

    try:
        return func.HttpResponse(translations)
    except Exception as e:
        return func.HttpResponse(
            f"Could not parse request. Error: '{e}'", status_code=400
        )

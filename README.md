# Azure DevOps - Automated ticket translator

## The Challenge

**Internationally operating teams** and service desks often **struggle with language barriers**, especially when using ticket systems.

The translation of tickets and messages can be **time-consuming** (copy and paste the text into the browser) and **costly** (professional translation services). Furthermore, the mileage may vary when comparing the **quality** of different translation services, and **domain-specific terminology** makes translation even more difficult. To make things more complicated they probably **do not want their confidential information translated by public web services**.

These teams need an **affordable,** **high-quality**, and **compliant** solution.

## The Solution

This repository contains a **simple step-by-step instruction** and **code** for **your own DevOps Ticket Translation Solution**.

![Animation](/docs/images/animation.gif)

Using **Azure Cognitive Services** and **Azure Functions**, this solution is **very affordable and easy to implement**. All you need is an Azure Subscription and - of course - Azure DevOps.

![Devops-Translator-Architecture](/docs/images/architecture_devops_translator.png)

The Cognitive Services Translator uses Microsoft's **high-quality neural translation models**, offering translations between **more than 60 languages** and also the possibility to **train your [Custom Translator](https://docs.microsoft.com/en-us/azure/cognitive-services/translator/translator-info-overview#language-customization) with domain-specific terminology**.

Last but not least, as both Azure Functions and Cognitive Services are **stateless and certified** (functions | [translator](https://www.microsoft.com/en-us/translator/business/notrace/#compliance)), and due to the Translator's **[no-trace](https://www.microsoft.com/en-us/translator/business/notrace/) guarantee,** this solution will certainly **comply with your company and state regulations**.

### A word about cost

Using pay-as-you-go services might cause some unease as the monthly bill becomes unpredictable. The worry is unfounded!

As long as you stick to standard language models and **stay below 1-2 thousand(!) translations per month** (at 1000 characters per ticket) **you don't pay a cent!** (or any currency you prefer).

The Cognitive Services Translator offers a **[free tier](https://azure.microsoft.com/en-us/pricing/details/cognitive-services/translator/)** (2 million characters/month included). The Azure Functions consumption plan (pay as you go) also offers a **monthly free grant of 1 million requests and 400,000 GB-s resource consumption**.

In case you want customized translations or translate larger ticket volumes, both Functions and Custom Translator are **quite inexpensive** (see: [Translator](https://azure.microsoft.com/en-us/pricing/details/cognitive-services/translator/) | [Functions](https://azure.microsoft.com/en-us/pricing/details/functions/)).

# Step-by-step instruction

_Enough marketing - let's get busy ..._

By the way, the documentation for DevOps, Functions, and Cognitive Services is very detailed and well maintained. As the services and workflows might change in the future, we refer to the step-by-step guides for each service.

### Requirements

All we need to implement our solution are

- **Azure DevOps** with sufficient rights to _edit tickets_, create _Personal Access Tokens_ and add _custom fields_ to your tickets

and an

- **Azure Subscription** and sufficient rights to create resources.

### Step 1: Azure DevOps

In Azure DevOps, you will want to add a custom field as your translation target ("Translated Description") and create a Personal Access Token (PAT).

#### Add a custom field

We want to add a new custom field to our Tasks. When following the guide below make sure to add a **Text (multiple lines)** field called "**Translated Description**", otherwise you will need to do some adjustments later on: https://docs.microsoft.com/en-us/azure/devops/organizations/settings/work/add-custom-field

#### Creating a Personal Access Token (PAT)

A Personal Access Token will allow our Translation service to access our Projects and add translations to our Tickets. Please keep in mind, that the translation service will only be able to edit projects, that you have access to.

This guide explains how to create a PAT: https://docs.microsoft.com/en-us/azure/devops/organizations/accounts/use-personal-access-tokens-to-authenticate

**Make sure to keep your PAT.** You will not be able to retrieve it afterwards and need it for the next step.

### Step 2: Azure

Using Azure Resource Management (ARM) Templates makes deployment very simple.

Click the "Deploy to Azure" Button and follow the guidance below:

[![Deploy to Azure](https://aka.ms/deploytoazurebutton)](https://portal.azure.com/#create/Microsoft.Template/uri/https%3A%2F%2Fraw.githubusercontent.com%2Fjplck%2Fdevops-ticket-translation-function%2Fpython-deployment-button%2Fazuredeploy.json)

* Select a Region that's closest to you or your customers
* Enter the Azure DevOps **Personal Access Token**
* Translator Location: Stick with 'global' unless you have specific requirements (e.g. GDPR).
* SKU : The pricing tier of the Cognitive Service Translator. Stick with F0 if 2M chars/months suffice and there is no other F0 resource in your subscription.
* Azure Function Runtime: Preferrably use dotnet (C#) as Python deployment takes a bit extra effort.

#### Step 2b (optional): Deploy Python Azure Function

The Python function cannot be deployed from GitHub (therefore dotnet deployment is preffered).

Clone this repository and publish the **parse-ticket** function from the python directory as in these guide for [Visual Studio](https://docs.microsoft.com/en-us/azure/azure-functions/functions-create-your-first-function-visual-studio), [Visual Studio Code](https://docs.microsoft.com/en-us/azure/azure-functions/functions-create-first-function-vs-code), and [the command-line interface](https://docs.microsoft.com/en-us/azure/azure-functions/functions-create-first-azure-function-azure-cli)

If you want to debug, make sure to add environment variables (see below) by replacing your local.settings.json with the [example file](python/example_local.settings.json) we provided and updating the fields (see the section on Function App Setting below).

### Step 3: Azure DevOps WebHook

We now have everything we need to translate our tickets. In Azure DevOps under **Project Settings > Service Hooks**, create a **new subscription**. Select **WebHook**, then **next**.

<img src="docs\images\service_hooks.png" alt="service_hooks" style="zoom:50%;" />

Chose "Work item updated" as the trigger, leave the rest at default values.

<img src="docs\images\trigger.png" alt="trigger" style="zoom: 67%;" />

Our action will be sending the ticket to our Azure Function, so all we need is to add the function URL:

<img src="docs\images\action.png" alt="action" style="zoom:67%;" />

Test the connection. If the URL is correct and the function is running you should receive a success message and you can finish the WebHook.

<img src="docs\images\test_webhook.png" alt="action" style="zoom:67%;" />

#### Testing the translation

To test your new translation service, go to your Project **Boards > Work** items and create a new item. Add some description that you would like to translate, and hit "save" and observe your **Translated Description** field. The translation should appear within a few seconds (2-10s).

If no translation appears, it's time for debugging. Go to step 5.

### (optional) Step 4: Azure Function Live Monitoring

https://docs.microsoft.com/en-us/azure/azure-functions/functions-monitoring

### (optional) Step 5: Custom Translator

https://portal.customtranslator.azure.ai/

https://docs.microsoft.com/en-us/azure/cognitive-services/translator/custom-translator/how-to-create-project

## Troubleshooting

#### DevOps Webhook Documentation

https://docs.microsoft.com/en-us/azure/devops/service-hooks/services/webhooks?view=azure-devops

#### Azure Function Documentation

https://docs.microsoft.com/en-us/azure/azure-functions/

#### Cognitive Services Translator Documentation

https://docs.microsoft.com/en-us/azure/cognitive-services/translator/

#### Cognitive Services Custom Translator Documentation

https://docs.microsoft.com/en-us/azure/cognitive-services/translator/custom-translator/overview

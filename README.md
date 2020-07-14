# Azure DevOps - Automated ticket translator

## The Challenge

**Internationally operating teams** and service desks often **struggle with language barriers**, especially when using ticket systems.

The translation of tickets and messages can be **time-consuming** (copy and paste the text into the browser) and **costly** (professional translation services). Furthermore, the mileage may vary when comparing the **quality** of different translation services, and **domain-specific terminology** makes translation even more difficult. To make things more complicated they probably **do not want their confidential information translated by public web services**.

These teams need an **affordable,** **high-quality**, and **compliant** solution.

## The Solution

This repository contains a **simple step-by-step instruction** and **code** for **your own DevOps Ticket Translation Solution**.

![Animation](/docs/images/animation.gif)

Using **Azure Cognitive Services** and **Azure Functions**, this solution is **very affordable** (most likely free) and **easy to implement**. All you need is an Azure Subscription and - of course - Azure DevOps.

![Devops-Translator-Architecture](/docs/images/architecture_devops_translator.png)

The Cognitive Services Translator uses Microsoft's **high-quality neural translation models**, offering translations between **more than 60 languages** and also the possibility to **train your [Custom Translator](https://docs.microsoft.com/en-us/azure/cognitive-services/translator/translator-info-overview#language-customization) with domain-specific terminology**.

Last but not least, as both Azure Functions and Cognitive Services are **stateless and certified** ([Azure Certificates and Compliance Offerings](https://azure.microsoft.com/en-us/resources/microsoft-azure-compliance-offerings/)), and due to the Translator's **[no-trace](https://www.microsoft.com/en-us/translator/business/notrace/) guarantee,** this solution will most certainly **comply with your company and state regulations**.

### Very affordable? A word about cost...

Using pay-as-you-go services might cause some unease as the monthly bill becomes unpredictable. The worry is unfounded!

As long as you stick to standard language models and **stay below 1-2 thousand(!) translations per month** (at 1000 characters per ticket) **you don't pay a cent!** The Cognitive Services Translator offers a **[free tier](https://azure.microsoft.com/en-us/pricing/details/cognitive-services/translator/)** (2 million characters/month included). The Azure Functions consumption plan (pay as you go) also offers a **monthly free grant of 1 million requests and 400,000 GB-s resource consumption**.

In case you want customized translations or translate larger ticket volumes translation won't be free, but both Functions and Custom Translator are **quite inexpensive** (see: [Translator](https://azure.microsoft.com/en-us/pricing/details/cognitive-services/translator/) | [Functions](https://azure.microsoft.com/en-us/pricing/details/functions/)).

# Step-by-step instruction

_Enough marketing - let's get busy ..._

### Requirements

All we need to implement our solution are

- **Azure DevOps** with sufficient rights to _edit tickets_, create _Personal Access Tokens_ and add _custom fields_ to your tickets

and an

- **Azure Subscription** and sufficient rights to create resources.

In case you want to edit your code and prefer to a Python runtime over C#, you'll probably want to use [VisualStudio Code](https://docs.microsoft.com/en-us/azure/azure-functions/functions-create-first-function-vs-code?pivots=programming-language-python), [Visual Studio](https://docs.microsoft.com/en-us/azure/azure-functions/functions-create-your-first-function-visual-studio) or the [Azure CLI in combination with Azure Functions Core tools](https://docs.microsoft.com/en-us/azure/azure-functions/functions-create-first-azure-function-azure-cli).

### Step 1: Azure DevOps

In Azure DevOps, we want to add a custom field as our translation target ("Translated Description") and create a Personal Access Token (PAT) to grant access to edit tickets.

#### Add a custom field

We want to add a new custom field to our Tasks. When following the guide below make sure to add a **Text (multiple lines)** field called "**Translated Description**", otherwise you will need to do some adjustments later on: https://docs.microsoft.com/en-us/azure/devops/organizations/settings/work/add-custom-field

#### Creating a Personal Access Token (PAT)

A Personal Access Token will allow our Translation service to access our Projects and add translations to our Tickets. Please keep in mind, that the translation service will only be able to edit projects, that you have access to.

This guide explains how to create a PAT: https://docs.microsoft.com/en-us/azure/devops/organizations/accounts/use-personal-access-tokens-to-authenticate

**Make sure to keep your PAT safe!** You will not be able to retrieve it afterwards and need it for the next step. If you lose your PAT somehow, you still can regenerate a new one.

### Step 2: Azure

We now want to deploy the Azure Resources (Translator, Function App). Using Azure Resource Management (ARM) Templates makes this deployment very simple. Just click the "Deploy to Azure" Button and follow the guidance below:

[![Deploy to Azure](https://aka.ms/deploytoazurebutton)](https://portal.azure.com/#create/Microsoft.Template/uri/https%3A%2F%2Fraw.githubusercontent.com%2Fjplck%2Fdevops-ticket-translation-function%2Fmaster%2Fazuredeploy.json)

- Resource Group: Create a new resource group and give it an informative name.
- Region: Select a Region that's closest to you or your customers and complies with your requirements.
- PAT: Enter your Azure DevOps **Personal Access Token**.
- Translator Location: Stick with 'global' unless you have specific requirements (e.g. GDPR).
- SKU : The pricing tier of the Cognitive Service Translator. Stick with F0 if 2M chars/months suffice and there is no other F0 resource in your subscription.
- Azure Function Runtime: Preferably use dotnet (C#). Python deployment is a bit more complicates and takes an extra step (see Step 2b).

#### Step 2b (optional): Deploy Python Azure Function

If you chose dotnet runtime, skip to step 2c.

The Python function cannot be deployed from GitHub. In order to deploy a Python function via Zip deployment, you'll want to clone this repository and publish the **parse-ticket** function from the python directory as in these guide for [Visual Studio](https://docs.microsoft.com/en-us/azure/azure-functions/functions-create-your-first-function-visual-studio), [Visual Studio Code](https://docs.microsoft.com/en-us/azure/azure-functions/functions-create-first-function-vs-code), and [the command-line interface](https://docs.microsoft.com/en-us/azure/azure-functions/functions-create-first-azure-function-azure-cli)

If you want to debug the function locally you'll need the function environment variables. Make sure to add environment variables by replacing your local.settings.json with the [example file](python/example_local.settings.json) we provided and updating the fields.

#### Step 2c: Retrieve Function URI

We now need to retrieve the Function URI.

Go to the [Azure Portal](https://portal.azure.com/), navigate to your **newly created resource group** and click on the **App Service resource**.

<img src="docs\images\select_appservice.png" alt="select_appservice" style="zoom:50%;" />

In the left blade click on **Functions** and then click on the function name (**webhook** if you deployed a dotnet function, **parse-ticket** if you deployed a python function).

<img src="docs\images\select_function.png" alt="select_function" style="zoom:50%;" />

In the Function Overview select **Get Function Url** and copy the function URL.

<img src="docs\images\get_function_url.png" alt="get_function_url" style="zoom:50%;" />

### Step 3: Azure DevOps WebHook

We now have everything we need to translate our tickets. In Azure DevOps under **Project Settings > Service Hooks**, create a **new subscription**. Select **WebHook**, then **next**.

<img src="docs\images\service_hooks.png" alt="service_hooks" style="zoom:50%;" />

Choose "Work item updated" as the trigger, leave the rest at default values.

<img src="docs\images\trigger.png" alt="trigger" style="zoom: 67%;" />

Our action will be sending the ticket to our Azure Function, so all we need is to add the function URL we copied earlier:

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

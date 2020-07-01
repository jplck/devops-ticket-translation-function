# Azure DevOps - Automated ticket translator

## The Challenge
**Internationally operating teams** and service desks often **struggle with language barriers**, especially when using ticket systems. The translation of tickets and messages can be **time-consuming** (copy and paste the text into the browser) and **costly** (professional translation services). Furthermore, the mileage may vary when comparing the **quality** of different translation services, and **domain-specific terminology** makes translation even more difficult.

Such teams look for both **affordable and high-quality solutions** and to make things more complicated they probably **do not want confidential information translated by public web services**.

## The Solution

This repository contains a **simple step-by-step instruction, how to build your own translation solution**.

*VIDEO OR GIF ANIMATION*

Using **Azure Cognitive Services** and **Azure Functions**, this solution is **very affordable and easy to implement**. All you need is an Azure Subscription and - of course - Azure DevOps. 

The Cognitive Services Translator uses Microsoft's **high-quality neural translation models**, offering translations between **more than 60 languages** and also the possibility to **train your [Custom Translator](https://docs.microsoft.com/en-us/azure/cognitive-services/translator/translator-info-overview#language-customization) with domain-specific terminology**.

Last but not least, as both Azure Functions and Cognitive Services are **stateless and certified** (functions | [translator](https://www.microsoft.com/en-us/translator/business/notrace/#compliance)), and due to the Translator's **[no-trace](https://www.microsoft.com/en-us/translator/business/notrace/) guarantee** this solution will certainly **comply with your company and state regulations**.

### A word about cost

We noticed that using pay-as-you-go services worries creates some uncertainty and worries as the monthly bill is unpredictable. There is no need to worry! Of course it depends on your workload but if you experiment of have a small project **you probably don't even have to pay a cent** ... or any currency you prefer.

The Cognitive Services Translator offers a **[free tier](https://azure.microsoft.com/en-us/pricing/details/cognitive-services/translator/)** (2 million characters / month included). The Azure Functions consumption plan (pay as you go) also offers a **monthly free grant of 1 million requests and 400,000 GB-s resource consumption**. 

If you want to go above those limits, both services are generally quite inexpensive ([Translator](https://azure.microsoft.com/en-us/pricing/details/cognitive-services/translator/) | [Functions](https://azure.microsoft.com/en-us/pricing/details/functions/)).

# Architecture
![Devops-Translator-Architecture](/docs/images/architecture_devops_translator.png)

## Step-by-step instruction

*Enough marketing - let's get busy ...*

### Requirements

You will need 
* Azure DevOps with sufficient rights to create *Personal Access Tokens* and add *custom fields* to your tickets and 
* an Azure Subscription and sufficient rights to create resources.

### Step 1 : Azure DevOps

### Step 2 : Cognitive Services

### Step 3a: Azure Function - Using C#

### Step 3b: Azure Function - Using Python
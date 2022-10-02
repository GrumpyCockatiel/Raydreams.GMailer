# Raydreams.GMailer

Examples using GMail API with .NET 6 starting with Bulk Forwarding.

## Tech Stack

C#/.NET 6

## Use Case

As a GMail user I want to bulk forward several emails from one of my GMail accounts to any other email address, preserving all the attachments and original sender. I don't want to use a 3rd party tool which might 'phone home' my emails because I'm not a gullible idiot.

## Overview

Bulk forward emails from a GMail account to any other email account. Email IDs that have been forwarded are stored in a local file so you can forward large mailboxes in multiple smaller runs.

Forwarding an email is really just making a copy of an existing message, replacing the TO header, stripping out the CC, and optionally just adding the original TO/FROM info to the body of the new message.

You can read more on the [GMail API Documentation](https://developers.google.com/gmail/api/reference/rest)

## App Configuration

You will need to add an `appsettings.json` file to your project with the added section **AppConfig** in the JSON root. You can hard code these values in the AppConfig Class to get started. I use appsettings over App.config so I can load by Configuation.

Just add a new file called `appsettings.json` to the Project Level and set it's **Copy to Output Directory** property to `Always Copy`. Add your own Client ID, Client Secret, GMail Account ID and address you want to forward to.

Copy and Paste the below and edit:

```
{
    "AppConfig": {
        "Environment": "DEV",
        "UserID": "bubba@gmail.com",
        "ForwardToName": "Bubba",
        "ForwardToAddress": "bubba@outlook.com",
        "ClientID": "<My GMail API Client ID>",
        "ClientSecret": "<My GMail API Client Secret>",
        "MaxRead": 1500,
        "MaxSend": 3,
        "SentFile": "MySentEmails"
    },
    "MIMERewriter": {
        "SubjectPrefix": "[My Tag]"
    },
    "Logging": {
        "Console": {
            "LogLevel": {
                "Default": "Information"
            }
        },
        "LogLevel": {
            "Default": "Warning"
        }
    }
}
```

## MIMEKit

This implementation uses [MIMEKit](https://github.com/jstedfast/MimeKit). You are not required to use MIMEKit. You could use AE.Net.Mail. You just have to take a RAW MIME message and replace a few fields which is easier to do with a MIME parser. I've abstracted away the MIME Parser so its easier to add whatever you like.

## History File

I simple forwarded emails txt file is saved to the Desktop for now. It's just easier to find while working on the app. Yes, a production app would save to a user's app space or have a Save File Explorer.

## GMail API Key

You will need a GMail API Client ID and Secret which you can create on Google Cloud.

### Create Project
Go to the [GCloud Developer Portal](https://console.cloud.google.com/) and **Create a New Project**. You may need to create an account first.

![Create Project](./readme/Screen%20Shot%201.png)

### Enable & Setup GMail API
Next enable the GMail APIs by clicking **Enable APIs and Services** searching for **GMail API** and finally clicking **Enable**.

![Enable GMail API](./readme/Screen%20Shot%202.png)

Then you need some credentials by going to the [Credentials Tab](https://console.cloud.google.com/apis/api/gmail.googleapis.com/credentials) and clicking **Create Credentials** and choosing the **OAuth Client ID** so users can login as specicfic users. However, you will be redirected to Configure a Login Consent screen first.

Then click **Configure Consent Screen** with **External** access and finally **Create**.

On the Consent Screen you need to provide an app name users will see when they login as well as some contact emails. You can also add a logo that will appear on the OAuth login pane.

![Consent Screen](./readme/Screen%20Shot%203.png)

Next you need to grant your app some scopes to what it can actually do. You can add all the scopes related to GMail to start with for playing around. However, you will need to be able to at lest read and send emails so make sure to include the **GMail https://mail.google.com/** scope. Click **Save and Continue** after adding scopes. Later you can edit the App and modify the scopes as well.

Next is to add users whose accounts you can access for testing. You will need to remember to add the Google User ID of any user you want to access their account. Users not listed here will not be able to log in with your app credentials until you publish the app (which is outside the scope of this example).

![Add Users](./readme/Screen%20Shot%205.png)

### Create Credentials

Now you can go back to **Create Credentials** and when you click Create choose the **OAuth Client** and application type (it really doesn't matter, choose desktop or web I guess) and give your client a name.

![Create Client](./readme/Screen%20Shot%206.png)

Finally, when you click Create you will get the **App Client ID** and **App Client Secret**.

![Client Secrets](./readme/Screen%20Shot%207.png)

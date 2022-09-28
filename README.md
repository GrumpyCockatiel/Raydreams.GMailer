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

## GMail API Key

You will need a GMail API Client ID and Secret which you can create on Google Cloud.

I'll add [intructions](https://developers.google.com/gmail/api/auth/web-server) on how to do this later but it's pretty much like creating any other API Key.

## App Configuration

You will need to add an `appsettings.json` file to your project with the added section **AppConfig** in the JSON root. You can hard code these values in the AppConfig Class to get started. I use appsettings over App.config so I can load by Configuation.

Just add a new file called `appsettings.json` to the Project Level and set it's **Copy to Output Directory** property to `Always Copy`. Add your own Client ID, Client Secret, GMail Account ID and address you want to forward to.

Copy and Paste the below and edit:

```
{
    "AppConfig": {
        "Environment": "DEV",
        "UserID": "tguillory@gmail.com",
        "ForwardToName": "Tag",
        "ForwardToAddress": "tag.guillory@outlook.com",
        "ClientID": "646684488144-9rvcfr7pv05s8iu6vrqf441d05ifsif2.apps.googleusercontent.com",
        "ClientSecret": "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",
        "MaxRead": 1500,
        "MaxSend": 3,
        "SentFile": "MySentEmails"
    },
    "MIMERewriter": {
        "SubjectPrefix": "[Test]"
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

This implementation uses [MIMEKit](https://github.com/jstedfast/MimeKit). You are not required to use MIMEKit. You could use AE.Net.Mail. You just have to take a RAW MIME message and replace a few fields which is easier to do with a MIME parser. I'll abstract away the MIME Parser later so its easier to use whatever you like.

## History File

I simple forwarded emails txt file is saved to the Desktop for now. It's just easier to find while working on the app. Yes, a production app would save to a user's app space.
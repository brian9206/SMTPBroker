# SMTPBroker
A simple local SMTP server that transform server alert email to Discord / Telegram message with attachment support.  

There is a web UI to let you view the full message which is larger than the destination maximum length limit or HTML message.

## Installation
The following shows the simplest setup, without any authentication.
For security reason, please do not expose your SMTP (port 25) to the Internet.

First, create `forwarder.yml`.
```yml
- name: Discord
  forwarder: discord
  parameters:
    webhook: YOUR_WEB_HOOK_URL

#- name: Telegram
#  forwarder: telegram
#  parameters:
#    bot_token: YOUR_BOT_TOKEN
#    chat_id: YOUR_CHAT_ID
```

Then, create data directory and docker container.
```bash
docker run -d \
  --name=smtpbroker \
  --restart=unless-stopped \
  -e Web__Url=https://your-ip:8443 \
  -v ./forwarder.yml:/forwarder.yml:ro \
  -v ./data:/data \
  -p 8443:443 \
  -p 127.0.0.1:25:25 \
  brian9206/smtpbroker:latest
```

### Authentication
You can set up SMTP or Web UI authentication by setting the environment variables

Add the following parameters to `docker run` command to enable authentication.
```bash
-e Web__Auth=true \
-e Web__User=YOUR_WEBUI_USERNAME \
-e Web__Password=YOUR_WEBUI_PASSWORD \
-e SMTP__Auth=true \
-e SMTP__User=YOUR_SMTP_USERNAME \
-e SMTP__Password=YOUR_SMTP_PASSWORD \
```

### Multiple forwarder
You can set up multiple forwarders and let them to handle different "From" or "To" addresses.

```yml
- name: Synology Notification
  forwarder: discord
  rules:
    from: "synology@example.com"
    stop: true
  parameters:
    webhook: DISCORD_WEB_HOOK

- name: Telegram
  forwarder: telegram
  parameters:
    bot_token: BOT_TOKEN
    chat_id: CHAT_ID
```

The above example will make all email from `synology@example.com` notified in Discord, others to Telegram.

Note: you can use wildcard like `*@example.com` and `to:` rule also works.

## How to obtain token in forwarder.yml?

### Discord Webhook
To get your Webhook URL, open your Discord and edit your channel.
Go to [Integration > Webhook > Create Webhook > Copy Webhook URL]

### Telegram BOT
To get your Telegram BOT token and Chat ID, please follow the following instruction.

1. Add `@BotFather`
2. Use `/newbot` command
3. Answer the questions, and you should have your BOT token.
4. Add `@getidsbot`
5. Use `/start` command
6. The ID under `You` is your chat ID.
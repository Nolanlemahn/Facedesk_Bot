# Facedesk_Bot
"Start before you’re ready." ― Steven Pressfield

`Facedesk_Bot` (FDB) is a C# bot thrown together using an old version of [Discord.Net](https://github.com/discord-net/Discord.Net). FDB is also made possible with [Cloud Firestore](https://firebase.google.com/docs/firestore/).

Configuring your own instance is left as an exercise to the reader.

## A warning

You should not trust me. You should host your own instance. This goes for absolutely every Discord bot ever written. Just because the source code does not indicate that I log your server does not mean that the actual bot will not log your server.

While I do insist that I am not logging your server and that I am a generally benevolent human being, you should take that with the same grain of salt as any other statement made by a stranger on the Internet. **Be careful.**

(An exception: if a command you invoke is invalid for any reason, the command will be logged.)

## Primary Setup

FDB will not respond to commands (asides from the authorization command itself) unless the commands are invoked from an authorized server.

### Authorization

`^auth YourKey` will authorize your server when invoked from any channel that FDB can read from. *I am not currently distributing authorization keys.* No particular permissions are required.

### Un/Subscribing to Announcements

`^setann #SomeChannelHere` will setup a channel for FDB to send announcements to. (Only one such channel may exist at a time.) These announcements will include notices of major updates and planned downtime. `^unsetann` will unsubscribe the channel. **You must be an Admin.**

### Expanding the 8ball

`^8balladd Code YourResponseHere` will add a response to the `^8ball` command. (There is currently no equivalent for removing responses.) **You must be an Admin.**

`Code` may be any character, but `+` and `-` are specifically allocated for positive and negative responses. All other codes are neutral. For responding, the following odds are observed.

Response | Odds
------------ | -------------
Positive | 25%
Negative | 25%
Neutral | 50%

Example: `^8balladd + Yes, that would be wise.`

## Setting up Channelmods

FDB facilitates a rudimentary workaround for Discord's lack of channel moderatorship. **The bot must be an Admin.**

`^cmod @someUser` will mark `@someUser` as a moderator for the channel in which the command was invoked. Repeating this will un-moderate the user.

### Channelmod commands

`^atc @someUser #someChannel` will grant `@someUser` basic permissions in `#someChannel`, provided that the invoker is a channelmod for that channel. (`#someChannel` is not optional.)

`^rfc @someUser #someChannel` will explicitly deny `@someUser` those same basic permissions, provided that the invoker is a channelmod for that channel. If `#someChannel` is not specified, the channel from which the command was invoked will be used.

## Other nonsense

`^batman` will list roles that are assigned to 0 users. No particular permissions are required.

`^opera @someRole` will assign a role to absolutely every user. **Both you and the bot must be an Admin.**

## Reporting bugs

I have no explicit formatting. You can track and submit long-term bugs or feature requests here: https://github.com/Nolanlemahn/Facedesk_Bot/issues

## Roadmap

I promise nothing.

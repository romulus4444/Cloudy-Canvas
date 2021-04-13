# Cloudy-Canvas
Discord bot for integration with the Manebooru imageboard

### Commands:
#### Booru Module:
**;pick <query>**
  * posts an image from Manebooru within the bot's filters matching the query
 
**;id <imageId>**
  * posts an image from Manebooru with the specified ID number

**;featured**
  * posts the current Featured Image from Manebooru

#### Admin Module
**;admin <commands>**
  * commands for managing the bot's settings
  * **;adminchannel <channel-name>**
    * Sets #channel-name as the admin channel. If <channel-name> is blank, it returns the currently set admin channel, if there is one.

**;blacklist <commands>**
  * manages the list of terms the bot will not search for
  * **;get**
    * gets the current list
  * **;add <term>**
    * adds <term> to the blacklist
  * **;remove <term>**
    * removes <term> from the blacklist
  * **;clear**
    * clears the blacklist of all terms

#### Info Module
**;help <command>**
  * Posts a list of commands
  * Posts more information about <command>

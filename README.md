# Simple Secure Chat (SSC)
An Instant Messaging solution based on simplify and secure ideas

# Get Start
**Server:**

Windows only(C#,DotNET): `src/SSC_Server`

Other Platform(Python): `src/SSC_Server_Lite`

**Client:**

Windows only(C#,DotNET 3.5+): `src/SSC_Client`

# SSC Protocol

Protocol Version: 0.1

**Basic Format**: `AES([requestType]|AES(requestData))\r\n`

* **requestTypes**
  * NICK: For client to set a unique nickname for itself in this session
    * This command send by client only
    * Client must set a nickname, otherwise server won't push any message or accept any other request from this client
    * Each client's nickname should be unique
    * Suggest server to keep some spacial first char to set Server/Admin apart from normal users like '@' and '&'
    * Possible response Info: ALREADY_SET, NICKNAME_EXIST, NICKNAME_NOT_ALLOW, OK
  * MSG: For client/server to send a *Message Type* data to server/client
    * Possible response Info: NICKNAME_NOT_SET
  * INFO: For server to send a response info of the last request from client
    * This command send by server only
    * response info is not necessary

* **Suggestions**
  * For security
    * When server cannot decrypt the request incoming correctly, server should drop the connection with client immediately
    * Server should set a connection time limit when client still don't have a nickname for preventing (D)DOS attack

# Welcome Pull requests and Issues.

# License
GPL v2

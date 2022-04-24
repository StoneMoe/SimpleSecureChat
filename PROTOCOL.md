## Protocol
Simple Secure Chat use MessagePack for data serialization
### Ver
1.0

### Message Layout
| Name   | Length   | Desc                         |
| ------ | -------- | ---------------------------- |
| Length | 4byte    | msg length                   |
| Data   | variable | msg data with AES encryption |

### Data Layout
*The number of parameters is variable and can be zero*
| Name   | Type     | Desc     |
| ------ | -------- | -------- |
| Type   | byte     | msg type |
| Params | object[] | params   |

### Message Type
* 0x00 HELLO: Certificate
  * Client
    * NoParam
  * Server
    * Param0: byte[] ECDSA X.509 Certificate, public key only
* 0x01 KEY: session key exchange
  * Client
    * Param0: byte[] BCRYPT_ECDH_PUBLIC_P521 blob
  * Server
    * NoParam
* 0x02 NICK: Broadcast chat message to everyone
  * Server
    * Param0: string result
      * "OK": nickname ok
      * "other text": error message
  * Client
    * Param0: string nickname
* 0x03 MSG: chat message
  * Server
    * Param0: string nickname
    * Param1: string messageText
  * Client
    * Param0: string messageText
* 0xA0 SYS: system message
  * Server
    * Param0: string messageText

# Simple Secure Chat
Setup your own encrypted chat room in seconds, on Windows/macOS/Linux.

## Feature
- [ ] Pre-shared key Authentication
- [ ] Server fingerprint identify
- [ ] Perfect Forward Security

## Quickstart
Download server and client executable from [release page](/releases/latest)

## Protocol
### Ver
1.0

### Message Layout
| Name   | Length   | Desc                         |
| ------ | -------- | ---------------------------- |
| Length | 4byte    | msg length                   |
| Data   | variable | msg data with AES encryption |

### Data Layout
*The number of parameters is variable*
| Name             | Length   | Desc                  |
| ---------------- | -------- | --------------------- |
| Type             | 1byte    | msg type              |
| Param n length   | 4byte    | param n data length   |
| Param n data     | variable | param n data          |
| Param n+1 length | 4byte    | param n+1 data length |
| Param n+1 data   | variable | param n+1 data        |

### Common Message Type
* 0x00 KX: Session key exchange
  * Param: EECDH Key
* 0x01 MSG: Broadcast chat message to everyone
### Client Message Type
* 0x50 CLIENT_HELLO: Client request server's certificate for identity check
* 0x51 NICK: Client trying to acquire a unique nickname with this type of message
  * Param: nickname text
### Server Message Type
* 0xA0 SERVER_HELLO: respond server certificate
  * Param: certificate
* 0xA2 RESP: respond to client's request
  * Param: response text

## Contribute
You can contribute code directly, or submit issue you found

## License
GPL v3

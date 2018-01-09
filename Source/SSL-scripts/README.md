Scripts
=====================

1. **GenerateTestCertificate**    
    Args: [].  
    *Generates a test certificate using a user data. (requires OpenSSL)*

2. **SetSsl**   
    Args: *pfxPath* - PFX certificate path, *password* - PFX certificate password, *port* - HTTPS port.  
    *Imports the specified certificate into the local store and makes the corresponding HTTPS reservation.*

**Remarks**  
+ To create a test certificate OpenSSL is needed.

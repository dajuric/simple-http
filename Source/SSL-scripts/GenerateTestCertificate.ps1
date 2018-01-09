#generates a test certificate

#install openssl (e.g.):
#   Set-ExecutionPolicy Bypass -Scope Process -Force; iex ((New-Object System.Net.WebClient).DownloadString('https://chocolatey.org/install.ps1'))
#   choco install openssl.light

openssl req -x509 -newkey rsa:1024 -keyout key.pem -out cert.pem
openssl pkcs12 -export -in cert.pem -inkey key.pem -out MyCert.pfx

#----------------------------
#reference: 
#  https://coderead.wordpress.com/2014/08/07/enabling-ssl-for-self-hosted-nancy/
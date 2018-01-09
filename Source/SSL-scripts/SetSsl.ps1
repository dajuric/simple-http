#imports the specified certificate into the local store and makes the corresponding HTTPS reservation

Param(
  $pfxPath,  #pfx file path
  $password, #pfx password
  $port      #HTTPS port
)

#expand the path to full path
$pfxPath = Resolve-Path $pfxPath
$pfxPath = $pfxPath.Path

#import certificate into the local store
Write-Output "Importing certificates..."
$password = ConvertTo-SecureString $password -AsPlainText -Force
$cert = Import-PfxCertificate -FilePath $pfxPath `
                              -CertStoreLocation cert:\localMachine\my `
							  -Password $password

#make HTTPS reservation and tie it with the certificate thumb-print
Write-Output "Removing existing HTTPS reservation (if any)..."
netsh http delete urlacl url=https://+:$port/
netsh http delete sslcert ipport=0.0.0.0:$port

Write-Output "Adding new HTTPS reservation..."
$appId = [guid]::NewGuid()
netsh http add urlacl url=https://+:$port/ user="Everyone"
netsh http add sslcert ipport=0.0.0.0:$port certhash=$cert.Thumbprint appid="{$appId}"

Write-Output "Done."

#----------------------------
# see also: 
   #https + Nancy      https://coderead.wordpress.com/2014/08/07/enabling-ssl-for-self-hosted-nancy/
   #cert + powershell  https://mcpmag.com/Articles/2014/11/18/Certificate-to-a-Store-Using-PowerShell.aspx?Page=1
   #get thumbprints    https://blogs.technet.microsoft.com/tune_in_to_windows_intune/2013/12/10/get-certificate-thumbprint-using-powershell/
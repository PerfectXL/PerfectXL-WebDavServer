# PerfectXL WebDAV Server

This Windows service is a stand-alone WebDAV server with local file
storage and basic authentication. It is a simple service that can
perform the file storage function in the PerfectXL Server Edition
ecosystem.

The service works via HTTPS. This encryption layer is necessary
because with basic authentication the password is sent in the HTTP
header unencrypted.

This software uses various open source components and was released
with a MIT license.

## Confirm that it works

If you have curl (<https://curl.haxx.se/windows/>), you can test the
server:

```bat

curl -k --basic -u PerfectXL:PerfectXL -X PROPFIND https://localhost:52442/

curl -k --basic -u PerfectXL:PerfectXL -X MKCOL https://localhost:52442/123

curl -k --basic -u PerfectXL:PerfectXL -T README.md https://localhost:52442/123/README.md

curl -k --basic -u PerfectXL:PerfectXL https://localhost:52442/123/README.md

curl -k --basic -u PerfectXL:PerfectXL -X DELETE https://localhost:52442/123/README.md

curl -k --basic -u PerfectXL:PerfectXL -X DELETE https://localhost:52442/123

```

See also <https://www.qed42.com/blog/using-curl-commands-webdav>.

And if you don't have curl, you can run these Powershell commands:

```ps1
$hostname = "localhost"
$port = 52442
$user = "PerfectXL"
$password = "PerfectXL"
[System.Net.ServicePointManager]::ServerCertificateValidationCallback = {$true}
$wc = (New-Object System.Net.WebClient)
$wc.Headers.Add("Authorization", "Basic " + [System.Convert]::ToBase64String([System.Text.Encoding]::ASCII.GetBytes("${user}:${password}")))
[System.Text.Encoding]::ASCII.GetString($wc.UploadData("https://${hostname}:${port}/", "PROPFIND", [System.Text.Encoding]::ASCII.GetBytes("")))
```

Any XML output indicates success.

## Certificate binding

The application will generate a self-signed certificate and bind it
to its IP port. Should you ever want to remove the binding, use
`netsh`:

```bat

netsh http delete sslcert ipport=0.0.0.0:52442

```

(Note that the certificate and the binding are not removed when you
uninstall the program.)
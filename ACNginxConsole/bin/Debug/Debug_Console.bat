@ECHO off

::ArtCenter Live Nginx Error Console
::Version: 1.1 (Live Tutorial)


:errorlog

cls
::开启Nginx
start Start_Nginx.bat

TITLE Nginx Debug Console - Activated

color 47
ECHO ------------------------------
ECHO     Nginx Debug Console
ECHO     Art Center Modified
ECHO.
ECHO     Nginx is [ Activated ].
ECHO ------------------------------

::建立error对照样本
copy ".\logs\error.log" ".\logs\errorcomp1.log" /y

timeout /T 10


::结束Nginx
nginx -s stop

::建立调试后对照样本
copy ".\logs\error.log" ".\logs\errorcomp2.log" /y

cls
color 27
TITLE Nginx Debug Console - Deactivated

ECHO ------------------------------
ECHO     Nginx Debug Console
ECHO     Art Center Modified
ECHO.
ECHO     Nginx is [ Deactivated ].
ECHO ------------------------------
::对照
fc /ivg ".\logs\errorcomp1.log" ".\logs\errorcomp2.log"

mshta vbscript:msgbox("Test will be restarted after confirmation.",64,"Test Completed")(window.close)
timeout /T 5

goto errorlog
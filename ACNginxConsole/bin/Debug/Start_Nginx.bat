::ArtCenter Live Modified
::Version 1.1

@ECHO OFF
MODE CON COLS=50 LINES=6
::COLOR 1f
ECHO.
ECHO ¡@¡@¡@ Nginx is activated!
ECHO.
ECHO Nginx won't stop until you start Stop_Nginx.bat
ECHO.

ECHO %date:~,10% %time:~,8% >.\logs\start.log

nginx
exit
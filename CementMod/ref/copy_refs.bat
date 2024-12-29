set game_dir=%1
set mypath=%~dp0

xcopy %game_dir%\MelonLoader\net6\ "%mypath%\net6" /s /e /y
xcopy %game_dir%\MelonLoader\Il2CppAssemblies\ "%mypath%\Il2CppAssemblies" /s /e /y
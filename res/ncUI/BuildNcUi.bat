:: Собрать CUIX + CFG
call "..\..\3rdparty\ncad_UI_creator_60\NC_UI_Creator_App.exe" cadDevCourseNet_UiDef.xml

xcopy "..\icons\*.ico" "..\..\code\cadApiDevCourseNET\cadApiDevCourseNET\bin\D_Nc26\icons" /Y /I
xcopy "cadDevCourseNet_NcUiDef.*" "..\..\code\cadApiDevCourseNET\cadApiDevCourseNET\bin\D_Nc26" /Y /I
xcopy "cadDevCourseNetNc.package" "..\..\code\cadApiDevCourseNET\cadApiDevCourseNET\bin\D_Nc26" /Y /I






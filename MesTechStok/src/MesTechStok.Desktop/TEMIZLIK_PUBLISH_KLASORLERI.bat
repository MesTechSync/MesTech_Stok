@echo off
echo ==========================================
echo MESTECH PUBLISH KLASORLERINI TEMIZLE
echo ==========================================
echo.

echo [!] DIKKAT: Asagidaki klasorler silinecek:
echo - publish_database_fixed
echo - publish_debug_compare  
echo - publish_fixed
echo - publish_yeni
echo.

echo [✓] KORUNACAK KLASORLER:
echo - publish_final (CALISIRKEN)
echo - publish_final_new (TEMIZLENEN)
echo.

pause

echo [1] Gereksiz publish klasorlerini sil:
if exist "publish_database_fixed" rmdir /S /Q "publish_database_fixed"
if exist "publish_debug_compare" rmdir /S /Q "publish_debug_compare"
if exist "publish_fixed" rmdir /S /Q "publish_fixed" 
if exist "publish_yeni" rmdir /S /Q "publish_yeni"
if exist "publish_saglam" rmdir /S /Q "publish_saglam"
if exist "publish_ultimate_fix" rmdir /S /Q "publish_ultimate_fix"
echo.

echo [2] Kontrol - kalan publish klasorleri:
dir publish*
echo.

echo TEMIZLIK TAMAMLANDI!
echo.
pause

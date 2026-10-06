@echo off
py -3.11 -m pip install -r requirements.txt
py -3.11 -m pip install pyinstaller
py -3.11 -m PyInstaller --noconfirm --onefile --windowed --name PixelSpriteStudioV5 main.py
pause

import os
import urllib.request
import zipfile
import shutil
import io

PROJE_YOLU = r"d:\Unity_Projeler\BlokDunyasi\BlokDunyasi"
MUSIC_DIR = os.path.join(PROJE_YOLU, "Assets", "Audio", "Music")
SFX_DIR = os.path.join(PROJE_YOLU, "Assets", "Audio", "SFX")
os.makedirs(MUSIC_DIR, exist_ok=True)
os.makedirs(SFX_DIR, exist_ok=True)

# İndirilecek ZIP dosyalarının doğrudan adresleri (CC0 / Telifsiz)
KENNEY_UI_URL = "https://kenney.nl/data/zip/kenney_ui-audio.zip"

headers = {'User-Agent': 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36'}

def download_and_extract_sfx(url, target_names_map, dest_dir):
    try:
        print(f"Indiriliyor: {url}")
        req = urllib.request.Request(url, headers=headers)
        with urllib.request.urlopen(req) as response:
            with zipfile.ZipFile(io.BytesIO(response.read())) as z:
                for zip_path in z.namelist():
                    filename_in_zip = os.path.basename(zip_path)
                    
                    for target_name, search_keyword in target_names_map.items():
                        if search_keyword.lower() in filename_in_zip.lower() and filename_in_zip.endswith(".ogg"):
                            out_path = os.path.join(dest_dir, target_name)
                            if not os.path.exists(out_path): 
                                with z.open(zip_path) as source, open(out_path, "wb") as target:
                                    shutil.copyfileobj(source, target)
                                print(f"  -> Cikarildi: {target_name} ({filename_in_zip})")
    except Exception as e:
        print(f"X ZIP Indirme hatasi: {e}")

# Mapping: Hangi sfx hangi klasördeki .ogg dosyasına eşleşecek
ui_map = {
    "ui_click.ogg": "click1",
    "invalid_drop.ogg": "error1",
    "line_clear.ogg": "completed1",
    "combo.ogg": "magic1",
    "game_over.ogg": "lowDown"
}

def download_file(url, out_path):
    try:
        print(f"Indiriliyor: {os.path.basename(out_path)} ...")
        req = urllib.request.Request(url, headers=headers)
        with urllib.request.urlopen(req) as resp, open(out_path, "wb") as f:
            shutil.copyfileobj(resp, f)
        print(f"  -> Basarili: {os.path.basename(out_path)}")
    except Exception as e:
        print(f"  -> HATA: {e}")

print("=== Kenney (CC0) SFX Paketi İndiriliyor ===")
download_and_extract_sfx(KENNEY_UI_URL, ui_map, SFX_DIR)

# block_place için de bir sfx indirelim
kenney_wood = "https://raw.githubusercontent.com/KenneyNL/Audio-UI/main/Audio/click_002.ogg"
download_file(kenney_wood, os.path.join(SFX_DIR, "block_place.ogg"))

print("\n=== Müzikler İndiriliyor (FreePD CC0) ===")
music_urls = {
    "menu_music.mp3": "https://freepd.com/music/Blippy%20Trance.mp3",
    "gameplay_music.mp3": "https://freepd.com/music/The%20Looming%20Battle.mp3",
    "gameover_music.mp3": "https://freepd.com/music/Long%20Note%20Two.mp3"
}

for filename, url in music_urls.items():
    download_file(url, os.path.join(MUSIC_DIR, filename))

print("\nTUM ISLEMLER TAMAMLANDI!")

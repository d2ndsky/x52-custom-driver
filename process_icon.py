from PIL import Image
import os

def process_icon():
    # Target image based on "the second one" with airplane
    input_path = r"C:\Users\redro\.gemini\antigravity\brain\058bfeba-5644-449e-abfb-01c47fa9332e\ae52_app_icon_1771098789292.png"
    output_ico_path = r"c:\Users\redro\Desktop\Proyectos\Habilidades antigravity\apps\x52-custom-driver\app.ico"
    output_png_path = r"c:\Users\redro\Desktop\Proyectos\Habilidades antigravity\apps\x52-custom-driver\app_icon.png"

    if not os.path.exists(input_path):
        print(f"Error: Input file not found at {input_path}")
        # Fallback to search in directory if exact name is wrong
        return

    img = Image.open(input_path).convert("RGBA")
    datas = img.getdata()

    new_data = []
    # Simple white removal threshold
    # Since DALL-E icons often have a white background
    for item in datas:
        # Check for white (or near white)
        if item[0] > 240 and item[1] > 240 and item[2] > 240:
            new_data.append((255, 255, 255, 0)) # Transparent
        else:
             new_data.append(item)

    img.putdata(new_data)
    
    # Crop to content if needed (optional, but good if there's extra padding)
    bbox = img.getbbox()
    if bbox:
        img = img.crop(bbox)

    # Resize to square if strictly needed, though icons usually are
    size = max(img.size)
    final_img = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    final_img.paste(img, ((size - img.size[0]) // 2, (size - img.size[1]) // 2))

    # Save high-res PNG
    final_img.save(output_png_path)
    print(f"Saved cleaned PNG to {output_png_path}")

    # Save as ICO with multiple sizes
    final_img.save(output_ico_path, format='ICO', sizes=[(256, 256), (128, 128), (64, 64), (48, 48), (32, 32), (16, 16)])
    print(f"Saved ICO to {output_ico_path}")

if __name__ == "__main__":
    process_icon()

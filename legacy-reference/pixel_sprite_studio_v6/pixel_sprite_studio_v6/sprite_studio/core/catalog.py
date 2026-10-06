
SKIN_TONES = {
    "Sáng": ((255,222,200,255),(232,178,158,255)),
    "Trung bình": ((226,185,153,255),(197,145,116,255)),
    "Ngăm": ((190,145,110,255),(154,105,78,255)),
}

HAIR_PALETTES = {
    "Đen tím": ((44,46,78,255),(28,28,54,255),(98,108,158,255)),
    "Trắng bạc": ((222,226,240,255),(160,168,200,255),(255,255,255,255)),
    "Đỏ": ((190,70,60,255),(130,40,40,255),(240,140,110,255)),
    "Xanh lá": ((120,200,130,255),(70,150,90,255),(190,240,180,255)),
    "Nâu": ((112,76,58,255),(72,48,40,255),(168,116,86,255)),
    "Vàng": ((220,188,92,255),(154,120,48,255),(255,230,150,255)),
}

PALETTES = {
    "Mặc định": ((250,250,255,255),(196,206,232,255),(86,132,214,255),(56,92,170,255)),
    "Lạnh": ((116,164,230,255),(72,118,196,255),(46,86,160,255),(32,62,122,255)),
    "Hỏa": ((230,126,126,255),(186,84,84,255),(214,72,84,255),(150,44,50,255)),
    "U tối": ((96,100,118,255),(64,68,84,255),(52,58,80,255),(34,38,58,255)),
    "Mộc": ((166,210,160,255),(112,166,108,255),(96,180,110,255),(56,130,76,255)),
}

CLASS_OPTIONS = ["Kiếm tu", "Đan tu", "Phù tu", "Thể tu", "Du hiệp"]
CLASS_TEMPLATES = {
    "Kiếm tu": {
        "outer": "Kiếm tu", "main_hand": "Kiếm", "off_hand": "Không",
        "belt": "Đai ngọc", "cape": "Không", "effect": "Slash", "element": "Kim"
    },
    "Đan tu": {
        "outer": "Đan tu", "main_hand": "Không", "off_hand": "Hồ lô",
        "belt": "Đai vải", "cape": "Áo choàng ngắn", "effect": "Heal", "element": "Mộc"
    },
    "Phù tu": {
        "outer": "Phù tu", "main_hand": "Không", "off_hand": "Phù",
        "belt": "Đai vải", "cape": "Không", "effect": "Burst", "element": "Hỏa"
    },
    "Thể tu": {
        "outer": "Thể tu", "main_hand": "Đao", "off_hand": "Khiên",
        "belt": "Đai giáp", "cape": "Không", "effect": "Glow", "element": "Thổ"
    },
    "Du hiệp": {
        "outer": "Du hiệp", "main_hand": "Quạt", "off_hand": "Không",
        "belt": "Đai ngọc", "cape": "Áo choàng ngắn", "effect": "Spark", "element": "Thủy"
    },
}

BODY_OPTIONS = {
    "gender": ["Nam","Nữ"],
    "class_type": CLASS_OPTIONS,
    "body_type": ["Tiêu chuẩn","Mảnh","Đậm"],
    "skin_tone": list(SKIN_TONES),
    "head_shape": ["Tròn","Gọn"],
    "eye_style": ["Bình tĩnh","Vui","Lạnh lùng","Nghiêm túc","Buồn ngủ"],
    "nose_style": ["Chấm","Không"],
    "mouth_style": ["Trung tính","Cười","Nghiêm"],
    "hair_style": [
        "Búi cao","Tóc dài","Tóc ngắn","Đuôi ngựa","Tóc xõa",
        "Tóc dựng","Tóc lệch","Hai búi","Tóc mái dài","Tóc võ sĩ"
    ],
    "hair_color": list(HAIR_PALETTES),
    "direction": ["Down","Up","Left","Right"],
    "skin_variant": list(PALETTES),
    "aura": ["Không","Kim","Mộc","Thủy","Hỏa","Thổ"],
    "effect": ["Không","Slash","Glow","Spark","Burst","Heal","Shield"],
    "element": ["Kim","Mộc","Thủy","Hỏa","Thổ"],
}

EQUIPMENT_LIBRARY = {
    "head": ["Không","Băng trán","Mũ vải","Mũ giáp"],
    "inner": ["Không","Áo trong sáng","Áo trong tối"],
    "outer": ["Không","Kiếm tu","Đan tu","Phù tu","Thể tu","Du hiệp"],
    "chest_armor": ["Không","Giáp ngực nhẹ","Giáp ngực nặng"],
    "shoulder": ["Không","Giáp vai nhẹ","Giáp vai nặng"],
    "gloves": ["Không","Bao tay vải","Bao tay giáp"],
    "pants": ["Không","Quần tối","Quần sáng","Quần chiến"],
    "boots": ["Không","Giày vải","Giày da","Ủng giáp"],
    "belt": ["Không","Đai vải","Đai ngọc","Đai giáp"],
    "cape": ["Không","Áo choàng ngắn","Áo choàng dài"],
    "main_hand": ["Không","Kiếm","Đao","Thương","Quạt","Trượng"],
    "off_hand": ["Không","Khiên","Phù","Hồ lô"],
    "accessory": ["Không","Ngọc bội","Khuyên tai","Hồ lô"],
}

SLOT_LABELS = {
    "head":"Đầu",
    "inner":"Áo trong",
    "outer":"Áo ngoài",
    "chest_armor":"Giáp ngực",
    "shoulder":"Giáp vai",
    "gloves":"Bao tay",
    "pants":"Quần",
    "boots":"Giày/Ủng",
    "belt":"Đai",
    "cape":"Áo choàng",
    "main_hand":"Vũ khí chính",
    "off_hand":"Tay phụ",
    "accessory":"Phụ kiện",
}

GAME_BG = {
    "Cỏ": (72,120,76,255),
    "Đá": (110,112,118,255),
    "Đất": (120,88,64,255),
    "Tối": (38,42,58,255),
}

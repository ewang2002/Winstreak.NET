# This file takes in the ascii.png file and parses it, effectively mapping each character to a binary string.
# This binary string can be used in `BinaryToCharactersMap` located in the Constants.cs file so it can parse
# Minecraft screenshots.

import os
from PIL import Image, UnidentifiedImageError


def main():
    path = input("Location to MC ASCII image? ")
    if not os.path.exists(path):
        print("Path not found.")

    # open the image so we can begin
    try:
        img = Image.open(path)
    except UnidentifiedImageError:
        print("Is this a valid image?")
        return
    
    # Load the image.
    px = img.load()
    width, height = img.size

    all_parsed_chars = {}
    # We start at 16, which is the level that starts with the "!" in the screenshot.
    cur_y = 2 * 8
    # We only care about all letters and numbers and some symbols, so setting iterations to "6"
    # effectively means that we will go through every line between the one that starts with "!"
    # and the one that starts with "p" (both inclusive).
    iterations = 6
    # While iterations is > 0 (because iterations = 0 implies false)
    while iterations:
        cur_x = 0
        # For obvious reasons, don't go out of bounds.
        while cur_x < width:
            line_pixels = ""
            # Generally, each character is 8 x 8. Realistically, most will be 8 x 5 (height = 8, width = 5).
            for x in range(8):
                for y in range(8):
                    line_pixels += "1" if px[cur_x + x, cur_y + y] == (255, 255, 255, 255) else "0"
                # A valid parsed binary string is one that ends with "00000000" (denoting a blank vertical line)
                # And has a "1" in it (so that it actually recognized a non-transparent pixel).
                if line_pixels[-8:] == "00000000" and any(i == "1" for i in line_pixels):
                    all_parsed_chars[f"({cur_x / 8} {cur_y / 8})"] = line_pixels[:-8]
                    break

            cur_x += 8
            # End of while loop

        iterations -= 1
        cur_y += 8

    print(f"{len(all_parsed_chars)} Items Found.")
    for item in all_parsed_chars:
        print(f"{item} => {all_parsed_chars[item]}")
    # Wait for user input.
    input()


main()

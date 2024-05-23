from colorthief import ColorThief

color_thief = ColorThief('/Users/simendomaas/Documents/Skolearbeid/Masteroppgave/Media/IMG_9560.jpg')
# Get the dominant color
dominant_color = color_thief.get_color(quality=1)
print(dominant_color)

palette = color_thief.get_palette(color_count=2, quality=1)
print(palette)
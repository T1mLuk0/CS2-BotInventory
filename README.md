# CS2-BotInventory
A modified, standalone version of ianlucas/cs2-css-inventory-simulator designed to automatically assign random, fully localized inventories (skins, knives, gloves, stickers) exclusively to bots, without the need for any external database, website, or SQL backend.

This project demonstrates a deep integration with CounterStrikeSharp to manipulate bot inventory data directly in memory. It is intended for server administrators who want to enhance the visual presence of AI-controlled players in a private, controlled environment.

 Legal & Risk Disclaimer

Server Guideline Violation

This plugin modifies the cosmetic appearance of in-game items. As clearly stated in Valve's official CS2 Server Guidelines (https://blog.counter-strike.net/server_guidelines/), "Allowing players to claim temporary ownership of CS:GO items that are not in their inventory (Weapon skins, knives, etc.)" is a prohibited practice on community servers. By using this plugin, you acknowledge that you are knowingly violating these guidelines.

GSLT Ban & VAC Risk

Valve actively monitors community servers. Running this plugin on a server with a valid Game Server Login Token (GSLT) exposes you to a high risk of a GSLT ban, which will permanently prevent your Steam account from generating new tokens. Furthermore, while the plugin operates server-side, any action that modifies the intended game state carries a non-zero risk of a VAC ban for your server account.

User Responsibility

YOU, AND YOU ALONE, ASSUME ALL RISKS. The author assumes no liability for any account restrictions, VAC bans, GSLT bans, or any other penalties imposed by Valve Corporation. It is your responsibility to understand the consequences of violating the official server guidelines. It is strongly recommended to operate this plugin only on private, offline, or non-commercial servers, and to set the FollowCS2ServerGuidelines to false.

 New Feature: Full-Coverage Cosmetics & StatTrak System

This enhancement module brings richer personalization to your bot inventory simulator, now covering every player (bots and real humans) on the server with fully automated, configurable decorations.

·  Random Music Kits – Every player (including bots) will be assigned a random music kit on MVP or spawn, creating an immersive atmosphere.
·  Random Agent Models – Supports separate random agent skin assignments for CT and T sides, making bots visually diverse.
·  StatTrak™ Counters – All weapon skins can come with random StatTrak counters, adding a tangible feel to every kill.
·  Auto-Generated Configuration – On first run, plugin generates config.json with all toggles and probability settings (music pool, agent lists, StatTrak chance), no manual editing required.
·  Full Player Coverage – Both bots and real players benefit from these random assignments, enhancing overall gameplay variety.(GSLT WARNING)

This feature runs independently, integrates seamlessly with the existing inventory system, and requires no additional dependencies—just drop and play.

 Important Configuration (CounterStrikeSharp)

To ensure the plugin functions correctly and to mitigate some risks, you MUST modify your CounterStrikeSharp configuration.

1. Navigate to your server's csgo/addons/counterstrikesharp/configs/ directory.
2. Open the core.json file.
3. Locate the setting "FollowCS2ServerGuidelines" and set it to false.
4. Save the file and restart your server.

This setting disables specific features within CounterStrikeSharp that are designed to comply with Valve's guidelines, which would otherwise block the core functionality of this inventory simulator.

 Compatibility with Base Inventory Simulator

This project is a direct modification of cs2-css-inventory-simulator and overrides its global inventory assignment logic. When you use this version:

· For Bots: All bots will receive the plugin's random, localized inventory. The base plugin's logic (web sync) is bypassed.
· For Real Players: This plugin does not assign any inventory to real players by default. They will see their own Steam inventories.
· To Restore Base Features for Yourself: If you are the server host and wish to use the original plugin's features (e.g., !ws to load your web-configured inventory), you can still do so. The commands and base logic of cs2-css-inventory-simulator remain intact. Your inventory will simply be a separate, manually controlled layer on top of the bot simulator.

This makes the project ideal for a server admin who wants a rich visual experience for bots while keeping their own inventory authentic and functional.

 Requirements

· CounterStrikeSharp (with FollowCS2ServerGuidelines set to false)
· Metamod:Source
· BotHider v0.1.3 (or compatible) – required to ensure bots are correctly identified and targeted by the inventory replacement logic. This plugin works in tandem with BotHider to isolate bot entities.

 Installation

1. Ensure all Requirements are correctly installed on your CS2 server.
2. Download the latest BotInventory.dll from the Releases page.
3. Place the .dll file into your server's csgo/addons/counterstrikesharp/plugins/BotInventory/ directory.
4. Start or restart your CS2 server.

 Usage

No console commands are required for the core functionality. The plugin activates automatically.

· Automatic Bot Inventory: As soon as a bot spawns, it will be assigned a full random inventory.
· Verification: You should see log output in your server console confirming that an inventory has been generated for bots.
· Bypass for Yourself: Use the standard !ws command from the base cs2-css-inventory-simulator to load your personal inventory from the web service. This will override the bot-only random assignment for your player only.
. You can find a Json profile with the path "\SteamLibrary\steamapps\common\Counter-Strike Global Offensive\game\csgo\addons\counterstrikesharp\configs\plugins\Botinventory\BotInventory.json" which be a cosmetic configuration allow you to define the style

 License

This project is licensed under the MIT License. See the LICENSE file for details. The original cs2-css-inventory-simulator by Ian Lucas is licensed under MIT, and all credit for the base system belongs to the original author.

 Acknowledgements

· ianlucas for the original cs2-css-inventory-simulator which served as the foundation.
· ed0ard for the BotRandomizer which provided inspiration for the approach to bot-only skin assignment.
<img width="1280" height="1024" alt="20B2CD~1" src="https://github.com/user-attachments/assets/f6da11f4-51be-4ee7-8a29-8d7c85b12809" />

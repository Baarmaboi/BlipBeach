BongoBeach/Blip Beach is a 3D 3rd person metal detecting game, where the player helps beach goers to find their missing items.
Controls: Movement (WASD), Camera (Mouse), Jump (Space), Sprint (Shift), Metal Detect (Mouse Left, toggle detectin mode on/off), Interact (E)
Win Condition (Unlock all Areas on Blip Beach by gaining all the Stars by finding all Lost Items for beach goers)
Next feature to build:
Enhanching quest systems and NPC interactions, Lost Item/Stars/Gated area unlock system.

Long term design goals:
Three main game loops
1. Searching for Lost Items for beach goers, gaining stars to unlock new areas.
Explanation: The goal of BlipBeach is to find all the Lost Items for all the beach goers. Beach goers are scattered on the beach each day and player can ask them if they need help. A beach goer who needs help explains what item they have lost and about where they lost it. This goes in to the players task menu (marked Item name and description, who lost it and about where they lost it). The player can then detect the item. After finding it they return it to the beach goer and gain a star. On the next day the beach goer goes and does something else, effects another beach goer, or something else happens, depending on weather the player found their lost item or not. Not all beach goers effect things in the world or other beach goers, but we want a sense of a progressing narrative trough out us finding Lost Items.

2. Searching for Treasures to sell to upgrade players equipment, like to make Metal Detector better etc.
Explanation: Random treasures are scattered in areas on the beach. Player can freely detect them and these items are not tied to any beach goer and are not Lost Items. There are 7 metal types (iron, copper, brass, aluminum, lead, silver, gold). All Treasures have an item name, depth and metal type (example: GOLD ring, IRON nail). All items can be sold at the market for cash to buy upgrades. For Iron, Copper, Brass, Aluminum and Lead, the items value depends on the Market Value at that time. Market value fluctuates with time for these metals. Silver and Gold treasures are always valuable, but are much more rare. Treasures are found in 3 depths; 0-15cm, 15-30cm and 30-50cm. Player can upgrade his detector from a shop with depth increase modules which increase the detection range; first from the default depth (0-15) to 0-30cm and then to the max depth of 50cm. The player can chooce which of the three depths to detect on, or to detect in the full available range.

Where you can find each item/metal type is tied loosely to a location:
- Iron (Nails, tools, bottle caps, machinery) is found at job site, industrial locations and sometimes on the beach near water
- Copper (coins, wire, fittings) is found at market areas where money is handled, hotels, booths, tikihuts and the beach
- Brass (jewelry, bucklers, fittings) is found mainly at the beach most commonly
- Aluminum (cans, pull tabs, foil) is found at secluded beach areas, drinking spots, beach near the water and the pier
- Lead (fishing) is found at fishing spots, beach water and the pier
- Gold and Silver is found rarely everywhere.

The goal is that upgrading your equipment depends on understanding the market and searching for the right materials to sell at the right time. Goal is also that certain more accurate equipment os needed for some later Lost Items to be found at all.

3. Day Loop, where one day lasts for a set amount of hours, giving the player a soft time limit to search for Lost Items. There would be a set amount of Days (3 for starters like in Majoras Mask). After that amount of days the game would loop to day 1.
Explanation: Each day would have beach goers doing different things in different places, and depending on which Lost Items the player has found the beach goers would be doing different things. Example: Player needs to finds a beach goers Lost Item (gift for girlfriend). He finds and returns it, and the next day the same beach goer is hanging out with his girlfriend, because the Lost Item was found the previous day. If the Lost Item was not found, the beach goer would be alone and sad because his girl friend dumped him, barring the player from further Lost Item searches from this person, or his grilfriend. 
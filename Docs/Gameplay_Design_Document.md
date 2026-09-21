# Gameplay Design Document

## 1. Tam nhin

### Ten tam thoi

**Beginner Village RPG**

### Trai nghiem cot loi

Nguoi choi dieu khien mot nhan vat trong the gioi realtime theo phong cach MMORPG 2D/side-view nhu Ninja School:

- Bat dau tai lang tan thu.
- Gap NPC, nhan nhiem vu va theo doi cot truyen.
- Di chuyen qua cac map va dung cong teleport de mo rong khu vuc.
- Khi den gan quai, he thong tu dong chon muc tieu va tan cong.
- Nguoi choi van quyet dinh huong di chuyen, muc tieu, ability loadout va cach phat trien nhan vat.
- Quai roi do, nguoi choi nhat do, len cap va tiep tuc chuoi nhiem vu.

Game khong con la card game va khong dung luot combat. Combat, status, cooldown va resource deu chay theo realtime clock.

### Pham vi ban dau

- Unity, single-player offline-first.
- Mot nhan vat dieu khien truc tiep.
- Mot lang tan thu lam hub dau tien.
- Mot den hai map train co quai.
- NPC, quest, teleport, loot, level va ability active/support.
- Chua can multiplayer, trade, guild hay PvP trong MVP.

## 2. Core loop

```text
Load character
    -> Spawn tai Beginner Village
    -> Nhan quest tu NPC
    -> Di chuyen den cong teleport
    -> Chon map va spawn tai diem an toan
    -> Di chuyen trong map
    -> Phat hien quai trong attack range
    -> Auto combat: target -> cast -> hit -> cooldown
    -> Quai chet -> drop loot / XP / quest progress
    -> Nhat loot va cap nhat inventory
    -> Du XP -> level up va cap nhat stats
    -> Hoan thanh quest -> mo quest/map/story tiep theo
```

### Nguyen tac dieu khien

- Movement luon uu tien hon auto combat.
- Player co the bat/tat auto combat.
- Player co the uu tien mot muc tieu hoac khu vuc.
- Player co the dung, doi target, doi ability loadout va roi combat.
- Auto combat khong duoc tu y teleport, nhat item quest quan trong hoac bo qua story choice.
- Tat ca action deu phai co cancel/interruption rule ro rang.

## 3. World va map

### MapDefinition

Moi map la mot data asset chua:

```text
mapId
displayName
sceneReference
spawnPoints
teleportGates
npcPlacements
monsterSpawnRegions
lootRules
recommendedLevel
unlockRequirements
```

### Beginner Village

Lang tan thu la safe hub dau tien:

- Khong co quai thuong tan cong player.
- Co NPC huong dan, quest giver, shop, inventory/storage va cong teleport.
- Co diem spawn va diem hoi phuc.
- Story quest dau tien dan player qua movement, auto combat, loot, level va teleport.

### Teleport gate

Cong teleport chi hoat dong khi player dung trong trigger va tuong tac, map dich da unlock, player khong bi combat lock neu design yeu cau, va khong dang trong action khong the cancel.

Teleport phai luu map hien tai, vi tri spawn va trang thai quest truoc khi chuyen scene.

### Streaming va scene boundary

MVP co the dung scene transition don gian. Ve sau co the chuyen sang additive loading:

```text
World map
    -> Load map scene
    -> Spawn player tai checkpoint
    -> Bind NPC / monster / teleport runtime
    -> Rebuild character modifiers
```

## 4. Player movement va interaction

### Movement

- Input movement tach khoi combat input.
- Player co toc do di chuyen, collision va animation state.
- Player khong bi auto combat keo di khoi vi tri neu khong co rule chase ro rang.
- Chase distance co gioi han; vuot gioi han thi quai reset target.

### Interaction

Player co the tuong tac voi NPC, teleport gate, shop/storage, quest object, loot item tren dat va door/map object co dieu kien.

Interaction duoc uu tien theo khoang cach va context. Khong tu dong nhat item neu player dang trong interaction khac.

## 5. Realtime combat

### Combat lifecycle

```text
Idle
    -> Detect target
    -> Acquire target
    -> Move into range (neu cho phep)
    -> Cast / basic attack
    -> Resolve hit
    -> Apply damage, heal, status, effect
    -> Wait cooldown / recovery
    -> Re-evaluate target
```

### Auto combat

Auto combat controller co cac che do:

- `Off`: khong tu dong tan cong.
- `Basic`: chi dung basic attack.
- `SkillRotation`: dung ability theo thu tu va dieu kien.
- `FullAuto`: tu chon target va ability theo rule.

Rule target co the gom gan nhat, dang danh player, HP thap nhat, elite/boss uu tien hoac target cua player.

### Range va timing

Moi attack co `range`, `castTime`, `recoveryTime`, `cooldown` va `interruptPolicy`.

- Khong apply hit truoc khi cast time hoan tat.
- Target chet truoc impact thi hit bi huy hoac doi target theo cau hinh ability.
- Ability snapshot gia tri tai thoi diem cast.
- Damage over time snapshot damage va duration khi effect duoc tao.

### Combat pipeline

```text
Validate action
    -> Snapshot ability/loadout/support
    -> Pay mana / reserve resource
    -> Cast animation
    -> Create AbilityIntent
    -> CombatSystem resolve hit
    -> Apply status/effect
    -> Floating text / VFX / SFX
    -> Death check
    -> Reward and quest event
```

`CombatSystem` la authority cuoi cung cho damage va resource delta. UI, auto combat va quest khong tu tinh damage rieng.

## 6. Ability va support loadout

### AbilityDefinition

Ability asset la immutable definition:

```text
abilityId
displayName
tags
activationMode
damage / heal / radius / duration
manaCost / reserveManaCost
cooldown
onCast
onHit
```

Ability khong luu support dang trang bi cua tung character.

### AbilityLoadout

Support duoc gan runtime theo kieu socket/loadout:

```text
AbilityLoadout
├── activeAbility
└── supportLinks[]
```

Khi cast:

1. Lay active ability.
2. Lay support trong loadout.
3. Loc theo required/excluded tags.
4. Bo duplicate theo rule first occurrence wins.
5. Tinh modifier.
6. Snapshot vao `AbilityIntent`.

Khong mutate `AbilityDefinition` asset trong runtime.

### Modifier support

Support definition chua tag condition va danh sach modifier asset:

```text
SupportAbilityDefinitionSO
├── requiredTags
├── excludedTags
└── modifiers[]
```

Modifier co the thay doi damage, heal, mana cost, duration, cooldown, projectile count, chain count, target limit va poison/burn/bleed tick damage.

Modifier tac dong vao DoT phai duoc snapshot khi cast, khong doc lai support moi tick.

## 7. Status, DoT va death

### Status timing realtime

Status co the dung seconds hoac combat ticks, nhung moi status phai chon mot don vi duy nhat:

- `OnApply`: chay ngay khi add.
- `OnTick`: lap theo tick interval.
- `OnExpire`: chay khi het han.

Vi du Poison:

```text
Base damage per tick: 10
Duration: 3 tick cycles
Support multiplier: +10%
Snapshot tick damage: 11
```

Poison instance luu `damagePerTick = 11` va `remainingDuration`. Thao support sau khi cast khong lam thay doi poison dang ton tai.

### Death check

- HP <= 0 thi entity vao dead state ngay sau pipeline hien tai.
- Entity dead khong duoc tao action moi.
- Quai dead phat reward event mot lan duy nhat.
- Player dead thi auto combat dung, loot pending duoc xu ly theo rule map.

## 8. NPC, quest va story

### NPCDefinition

```text
npcId
displayName
portrait
dialogueTree
shopDefinition
questOffers
services
```

NPC runtime chi giu state cua scene va lien ket den quest/progression system.

### QuestDefinition

```text
questId
title
description
prerequisites
objectives[]
rewards
nextQuests
storyFlags
```

Objective co the la noi chuyen voi NPC, di den map, giet so luong quai theo species/tag, nhat item, giet boss, dung teleport hoac dat level.

Quest progress phai nhan event tu world/combat/inventory thay vi quet scene moi frame.

### Story progression

Story flag luu rieng voi quest state. Map, NPC dialogue va teleport co the yeu cau flag, vi du `Quest_Tutorial_Completed`, `Boss_Forest_Defeated` hoac `Teleport_Mine_Unlocked`.

Khong dung level player lam dieu kien duy nhat neu story can mot hanh dong cu the.

## 9. Loot, inventory va progression

### Muc tieu cua inventory

Inventory la he thong luu tru va trang bi cac object ma player nhat duoc, mua duoc, nhan tu quest hoac mo khoa qua progression. Inventory khong tu tinh final stat va khong tu cast ability. No chi quan ly ownership, slot, stack, trang thai trang bi va phat event cho cac he thong khac.

### Loot flow

```text
Monster death
    -> Emit MonsterKilled
    -> Roll loot table mot lan
    -> Spawn WorldDrop instances
    -> Player tuong tac hoac auto-pickup theo rule
    -> Validate inventory capacity / stack
    -> Add ItemInstance vao inventory
    -> Emit ItemPickedUp
    -> Cap nhat quest progress
```

Quy tac:

- Moi monster death chi roll loot mot lan, ke ca monster bi xu ly lai boi scene event.
- World drop la runtime object; item trong inventory la instance duoc save.
- Item quest khong duoc mat neu inventory day; phai co error state va cach xu ly ro rang.
- Auto-pickup chi nhat item trong whitelist/cau hinh, khong tu y nhat item quest neu quest chua active.
- Loot roll khong ghi vao `ItemDefinition`; ket qua roll nam trong `ItemInstance`.
- Item duoc tao boi craft hoac loot phai duoc roll thanh `ItemInstance` mot lan truoc khi vao inventory.
- Moi lan roll phai co seed hoac roll metadata de debug va khong roll lai khi scene reload.

### ItemDefinition: static data

`ItemDefinition` la `ScriptableObject`, immutable trong runtime:

```text
itemId                 ID on dinh, khong doi sau khi release
displayName            ten hien thi
description            mo ta
icon                   icon inventory/world drop
itemType               Equipment, Consumable, Quest, Currency, Material
rarity                 Common, Magic, Rare, Legendary
tags                   Weapon, Fire, Quest, Material, ...
maxStack               gioi han stack; equipment thuong la 1
inventoryWidth/Height  dung neu sau nay co grid inventory
fixedAffixes            option co dinh theo archetype item
randomAffixPool         tham chieu den `RandomAffixPoolSO`
randomAffixRules        so luong prefix/suffix, tier, weight va dieu kien roll
useActions              action khi dung item
sellValue               gia ban neu item cho phep ban
```

`ItemDefinition` co the chua cac option co dinh dac thu cua item, nhung khong chua ket qua roll cua mot item cu the.

Vi du fixed affixes theo item archetype:

```text
Weapon  -> damage / attack speed / weapon range tuy loai vu khi
Armor   -> defense / max HP / resistance tuy loai ao giap
Ring    -> khong bat buoc co fixed combat stat
Potion  -> use action va effect definition
Quest   -> quest tag va ownership rule
```

`fixedAffixes` la modifier co dinh cua moi item instance cung definition. `randomAffixPool` va `randomAffixRules` chi la rule de tao ket qua; khong duoc doc truc tiep de tinh stat sau khi item da duoc roll.

`RandomAffixPoolSO` la asset co the tai su dung:

```text
RandomAffixPoolSO
├── prefixAffixAssets[] -> AffixDefinitionSO
└── suffixAffixAssets[] -> AffixDefinitionSO
```

`AffixDefinitionSO` la asset dung chung cho mot affix family/option:

```text
Affix_Int.asset
Affix_Str.asset
Affix_Dex.asset
Affix_AttackSpeed.asset
```

Vi du `Affix_Int.asset` co the duoc keo vao ca `Weapon_AffixPool`, `Armor_AffixPool` va `Ring_AffixPool`. Designer chi nhap tier/value/weight mot lan. Pool chi quan ly danh sach affix duoc phep xuat hien cho archetype do.

Workflow cua game designer:

1. Tao asset `Create -> TTH -> Game -> Inventory -> Random Affix Pool`.
2. Tao cac asset `AffixDefinitionSO` cho option dung chung, sau do cau hinh tier/value/weight mot lan.
3. Keo cac `AffixDefinitionSO` vao dung list Prefix hoac Suffix cua pool.
4. Mo `ItemDefinitionSO` va keo `RandomAffixPoolSO` vao field `Random Affix Pool`.
5. Cau hinh so luong affix trong `RandomAffixRules` cua item.

Mot pool co the duoc dung cho nhieu weapon. Pool weapon chung co the tai su dung; pool rieng cua item chi can tao khi item co candidate hoac weight khac. Duplicate `affixFamilyId` trong cung mot slot phai bi bao canh bao trong Inspector.

### ItemInstance: mutable state

```text
instanceId             ID duy nhat cua instance
definitionId           tham chieu den ItemDefinition
quantity               so luong trong stack
level                  cap item neu co
fixedAffixValues       gia tri fixed da snapshot neu co scaling theo item level
randomAffixes          danh sach affix da roll, gom affixId, affixFamilyId, slotType, tier va value
rollSeed               seed hoac roll id dung de audit/debug
durability             do ben neu co
isEquipped             trang thai trang bi
boundState             item bound character/account hay khong
acquiredAt             debug / analytics / save metadata
```

Affix va roll:

- Craft va loot goi cung mot `ItemRollService`, khong tu roll rieng trong UI, inventory hay monster.
- `ItemRollService` doc fixed affixes, random affix pool, rarity va roll rules de tao `ItemInstance`.
- Fixed affix luon co neu item archetype yeu cau; random affix co the khong co neu roll rules cho phep.
- Moi random affix phai luu `affixId`, `tier`, `value` va nguon roll trong instance.
- Sau khi roll, stat cua item chi doc snapshot trong instance; khong roll lai khi equip, load scene, save/load hoac rebuild modifier.
- Craft co the truyen recipe, material quality va guaranteed affix rules vao `ItemRollService`.
- Loot co the truyen loot table, monster tier, area level va rarity budget vao `ItemRollService`.
- Khong mutate `ItemDefinition`, affix definition hay random pool trong runtime.

Stacking:

- Material va consumable co the stack neu cung `definitionId`, cung fixed state, cung random affix state va cung rolled state.
- Equipment co bat ky random affix nao khac nhau khong duoc stack.
- Item bound khong duoc merge voi item non-bound.
- Merge stack phai xac dinh instance nao giu ID va metadata.
- `instanceId` khong duoc tao lai khi load save.

### Affix definition va roll rule

Affix la data asset rieng, khong nam truc tiep trong item instance:

```text
AffixDefinition
affixId
affixFamilyId
displayName
slotType                Prefix hoac Suffix
tags
targetItemTags
effectType              Attribute hoac Gameplay
attributeId             neu effectType = Attribute
modifierOperation       Add hoac Multiply
tiers[]

AffixTier
tierId
minValue / maxValue
weight
requiredLevel
```

Trong Unity, `AffixDefinitionSO` la asset chuan nam trong `RandomAffixPoolSO`, khong can tao lai option khi dung cho pool khac. Moi tier nam trong affix asset do va duoc tai su dung qua reference.

`displayName` chi dung cho UI. Affix logic phai dung `effectType`, `attributeId` va `modifierOperation`; khong duoc parse text de suy ra stat.

`affixId` la ID cua mot option cu the. `affixFamilyId` la nhom loai tru nhau trong cung item. Vi du cac tier `FireDamageFlat_T1`, `FireDamageFlat_T2` va `FireDamageFlat_T3` co the co `affixId` khac nhau nhung phai cung `affixFamilyId = FireDamageFlat`; item chi duoc co mot family nay.

```text
AffixSlotType = Prefix | Suffix
DuplicatePolicy = UniqueByAffixFamily
```

`RandomAffixRules` cua item hoac loot/craft recipe xac dinh:

```text
minRandomAffixes / maxRandomAffixes
minPrefixAffixes / maxPrefixAffixes
minSuffixAffixes / maxSuffixAffixes
allowedAffixTags
excludedAffixTags
allowedTiers
rarityWeights
duplicatePolicy
guaranteedAffixes
```

Rule phai validate required/excluded tags truoc khi roll. Affix da roll phai tuong thich voi item archetype; vi du affix `FireDamageFlat` co the yeu cau tag `Weapon` va khong ap dung cho ring neu design khong cho phep.

Rule MVP de xuat:

```text
minRandomAffixes = 1
maxRandomAffixes = 4
minPrefixAffixes = 0
maxPrefixAffixes = 2
minSuffixAffixes = 0
maxSuffixAffixes = 2
duplicatePolicy = UniqueByAffixFamily
```

Moi item co the co 0-2 prefix va 0-2 suffix, nhung tong so random affix phai tu 1 den 4. He thong phai chon mot cap prefix/suffix hop le truoc khi roll, khong roll hai slot doc lap roi retry vo han.

Rarity duoc suy ra tu tong so random affix va snapshot vao `ItemInstance`:

```text
1 affix -> Common
2 affix -> Magic
3 affix -> Rare
4 affix -> Legendary
```

Magic item co invariant bat buoc:

```text
Magic = 1 prefix + 1 suffix
```

Khong hop le neu item co 2 prefix, 2 suffix hoac mot slot rong. Roll service phai loai cac cap slot nay truoc khi chon affix.

`ItemDefinition` van co the khai bao rarity base/archetype de lam data metadata, nhung rarity cua item cu the sau craft/loot la `rolledRarity` trong `ItemInstance`.

### Affix roll algorithm

`ItemRollService` phai roll theo mot pipeline duy nhat:

```text
Validate item tags, rarity va roll budget
    -> Build candidate prefix pool
    -> Build candidate suffix pool
    -> Apply guaranteed affixes truoc
    -> Chon prefix count trong min/max
    -> Chon suffix count trong min/max
    -> Chon affix theo weight, moi affixFamilyId chi mot lan
    -> Chon tier va value trong tier range
    -> Validate total/prefix/suffix count va family uniqueness
    -> Snapshot ket qua vao ItemInstance
```

Quy tac bat buoc:

- Khong duoc so sanh trung lap bang `affixId` don thuan; phai unique theo `affixFamilyId`.
- Guaranteed affix chiem mot slot prefix/suffix va cung bi kiem tra trung family.
- Tong so affix sau khi roll phai nam trong khoang 1-4; rarity duoc gan theo tong so nay.
- Neu tong so affix la 2, shape bat buoc la 1 prefix va 1 suffix de tao Magic item.
- Khong du candidate pool hop le thi roll fail co ly do ro rang; khong retry vo han.
- Neu roll fail do recipe/loot rule thieu candidate, craft/loot khong duoc tao item khong hop le.
- Random affix duoc chon theo weight; tier value duoc roll doc lap trong range cua tier da chon.
- Seed/roll metadata phai duoc luu de replay va debug ket qua roll.

### Inventory container

```text
InventoryRuntime
├── containers: Bag, Equipment, Quest, Storage
├── itemInstances
├── capacity rules
└── ownership events
```

API toi thieu:

```text
TryAdd(ItemInstance)
TryRemove(instanceId, quantity)
TryMove(instanceId, container)
TryEquip(instanceId, slot)
TryUnequip(slot)
FindByDefinition(definitionId)
CanAdd(definitionId, quantity)
```

Moi API tra ve ket qua thanh cong/that bai kem ly do. Khong duoc silent fail khi inventory day, slot sai hoac item da bi xoa.

### Equipment slots

Equipment slot nen la data/config, khong hard-code vao UI:

```text
EquipmentSlotDefinition
slotId
acceptedTags
maxCount
```

Vi du slot `Weapon`, `Armor`, `Ring`, `Pet`. Item chi equip duoc neu tag cua item phu hop slot. Equip item moi phai:

1. Validate instance ton tai va chua bi bound sai.
2. Validate slot va dieu kien level/class/quest.
3. Unequip item cu neu rule cho phep.
4. Set instance state.
5. Rebuild character modifiers.
6. Emit `ItemEquipped`.

Unequip lam nguoc lai va phai remove modifier theo dung source cua item, khong clear toan bo modifier character.

### Consumable

Consumable khong phai equipment. Khi dung:

1. Validate target va dieu kien use.
2. Tru quantity sau khi action duoc accept, khong tru khi cast bi reject.
3. Tao temporary modifier/effect co source la item instance.
4. Emit `ItemUsed`.

Neu item co heal/damage, effect van di qua combat/effect pipeline hien tai. Inventory khong tu apply HP delta o mot code path rieng.

### Item modifier provider

Item cung cap modifier qua fixed affixes va random affixes da snapshot:

```text
ItemDefinition
├── fixedAffixes[]
└── randomAffixRules

ItemInstance
├── fixedAffixValues[]
└── randomAffixes[]
    -> AttributeModifier hoac GameplayModifier
```

`AffixModifierBridge` chuyen affix snapshot thanh runtime modifier:

```text
random affix Attribute
    -> AffixModifierBridge
    -> AttributeModifier(AttributeId, Operation, Value)
    -> AttributeSystem
```

Bridge gan `Source = ItemInstance` de unequip dung item chi remove modifier cua item do. Affix `Gameplay` khong duoc ep vao `AttributeId`; no di qua gameplay modifier context rieng.

Khi item equipped, he thong tao runtime modifier tu ca fixed affixes va random affixes voi:

- `Source = ItemInstance`.
- `StackKey` on dinh theo instance, affixId va source slot.
- `SourceType = Bonus` hoac type duoc cau hinh.
- `Order` theo pipeline resolve.
- `DurationSeconds <= 0` neu la modifier permanent khi equip.
- Modifier random da roll khong duoc doc lai random pool de tinh lai value.

Khi unequip, goi remove theo `Source = ItemInstance`. Khong luu truc tiep final ATK/DEF vao item hay `CharacterStatsSO`.

### Craft va loot item

Hai flow phai dung chung mot contract:

```text
Craft request / Loot request
    -> Validate recipe or loot rule
    -> Determine item definition and rarity budget
    -> Roll fixed affixes
    -> Roll random affix count and affixes
    -> Snapshot affix values, tier and roll metadata
    -> Create ItemInstance
    -> Validate inventory capacity
    -> Add item or retain world drop on failure
```

Craft recipe co the override mot phan roll rule, vi du dam bao `FireDamage` hoac tang tier toi da. Override chi ap dung trong lan craft do va ket qua van phai luu vao `ItemInstance`.

Khong cho phep craft/loot tao item bang cach sua `ItemDefinition`. Item definition chi la template; item instance moi la ket qua cu the cua craft hoac loot.

### Level va XP

Monster reward event cung cap experience, currency, loot va quest progress.

```text
MonsterKilled
    -> Validate reward owner
    -> Add XP
    -> Check level thresholds
    -> Apply progression modifiers
    -> Rebuild attributes
    -> Clamp current HP/MP theo rule
```

Level up khong ghi nguoc final stat vao `CharacterStatsSO`. Base/progression data va runtime modifier phai tach nhau.

## 10. Pet, constellation va character modifiers

### Nguyen tac chung

Item, pet, constellation, passive, achievement va temporary buff deu la `ModifierProvider`. Chung co the tao hai loai modifier:

1. `AttributeModifier`: thay doi ATK, DEF, HP, MP, SPD, DEX va stat tuong lai.
2. `GameplayModifier`: thay doi hanh vi nhu projectile count, chain count, target limit, loot rate hoac auto-combat rule.

`AttributeSystem` la authority duy nhat cho final character attributes. Gameplay modifier khong duoc ep thanh `AttributeId` chi de dung chung pipeline.

### ROTMG-style attributes

MVP quay lai mot he attribute don gian, doc lap va phu hop mobile:

```text
HP, MP, ATK, DEF, SPD, DEX, VIT, WIS
```

`AttributeSystem` doc final value truc tiep tu cac attribute nay. `DerivedStatSystem` chi phu trach cac gia tri phu can thiet cho realtime combat:

```text
FireRate  <- DEX
MoveSpeed <- SPD
HPRegen   <- VIT
MPRegen   <- WIS
```

Item affix dung chung mot mapping typed, khong parse display text:

```text
HP affix  -> HP
MP affix  -> MP
ATK affix -> ATK
DEF affix -> DEF
SPD affix -> SPD
DEX affix -> DEX
VIT affix -> VIT
WIS affix -> WIS
```

Affix prefix MVP:

```text
add HP, add MP, add DEF, add ATK
```

Affix suffix MVP:

```text
add WIS, add VIT, add DEX, add SPD
```

MVP khong roll elemental damage, crit multiplier, movement speed percent hay attack speed. Neu can them lai sau nay, them affix asset moi ma khong doi attribute core.

### Damage model mobile MVP

Elemental damage nen duoc giu vi no tao identity cho weapon va build ma khong can them he thong phuc tap. `FireDamage`, `ColdDamage` va `LightDamage` la gameplay modifiers duoc snapshot trong `ItemInstance`.

Damage penetration tam thoi khong them vao MVP. Basic damage dung `ATK` va mitigation dung `DEF`; sau nay co the them `ArmorPenetration` hoac `ElementalResistance` nhu gameplay modifier khi combat da on dinh.

### CharacterModifierRuntime


CharacterModifierRuntime
├── activeProviders
├── attribute modifiers
├── gameplay modifiers
├── source registry
└── rebuild / remove API
```

Responsibilities:

- Collect tat ca provider dang active.
- Xoa modifier cua provider cu truoc khi rebuild.
- Add modifier moi theo thu tu `Order`.
- Giữ source registry de debug va remove chinh xac.
- Khong add trung provider khi load scene lai.
- Emit `CharacterModifiersRebuilt`.

### PetDefinition va PetInstance

Pet la mot he thong progression/doc lap, khong phai item equipment co icon khac.

```text
PetDefinition
├── petId
├── displayName / icon
├── tags
├── baseModifierProviders
├── activeAbilityLoadout (optional)
└── activationPolicy

PetInstance
├── instanceId
├── definitionId
├── level / experience
├── bond / evolution state
├── equipped state
└── summoned state
```

Lifecycle pet:

- `Owned`: co trong collection, chua cho modifier equipped.
- `Equipped`: ap dung stat bonus passive.
- `Summoned`: co the ap dung active ability va modifier trong battle/map.
- `Inactive`: khong ap dung modifier.

Pet chi co mot instance active trong mot slot neu slot rule khong cho phep nhieu pet. Unequip/summon/desummon phai rebuild modifier theo source pet instance.

### ConstellationDefinition va ConstellationState

Constellation la progression tree, khong phai item stack:

```text
ConstellationDefinition
├── constellationId
├── nodes[]
└── unlock / activation rules

ConstellationNodeDefinition
├── nodeId
├── prerequisites
├── cost
├── tags
├── modifierProviders
└── passiveOrActivated

ConstellationState
├── unlockedNodes
├── activatedNodes
└── spentCurrency
```

Quy tac:

- `Unlocked` khong dong nghia `Active`.
- Node passive ap dung ngay khi unlock.
- Node activated chi ap dung sau khi player kich hoat.
- Reset node phai hoan tra/tieu currency theo rule da chot.
- Provider cua node co source rieng, vi du `Constellation_<id>_<nodeId>`.

### GameplayModifier

Gameplay modifier co contract rieng:

```text
GameplayModifier
├── modifierType
├── value
├── requiredTags
├── excludedTags
├── stackingPolicy
└── source
```

Vi du modifier:

- `ProjectileCount +1`.
- `ChainCount +1`.
- `TargetLimit +2`.
- `LootRate +10%`.
- `AutoCombatRange +20%`.
- `PoisonTickDamage +10%`.

Modifier ability support chi tac dong vao `AbilityLoadout` va snapshot vao `AbilityIntent`. Modifier character tu item/pet/constellation tac dong vao character runtime/context. Hai nhom khong duoc tu y mutate data asset cua nhau.

### Lifecycle va stacking

Moi provider phai khai bao lifecycle:

```text
Permanent   -> ton tai khi progression active
Equipped    -> ton tai khi item/pet equipped
Activated   -> ton tai sau khi node/feature duoc bat
BattleOnly  -> ton tai trong mot battle
Timed       -> het han sau duration
```

Stacking policy:

- `Stack`: instance hop le duoc cong don.
- `UniqueByStackKey`: cung key chi co mot modifier.
- `RefreshDuration`: duplicate refresh thoi gian.
- `IgnoreDuplicate`: bo qua duplicate.

Rule support hien tai la `IgnoreDuplicate`, giu support dau tien trong loadout. Rule nay khong tu dong ap dung cho inventory; hai item khac nhau co the cung tang ATK neu modifier cho phep.

### Rebuild contract

Moi thay doi sau phai rebuild character modifiers:

- Equip/unequip item.
- Activate/deactivate pet.
- Unlock/activate/reset constellation node.
- Level up.
- Add/remove temporary buff.
- Load save.
- Chuyen map neu modifier co lifecycle theo map.

Rebuild phai idempotent:

```text
Remove all modifiers owned by CharacterModifierRuntime
    -> Collect active providers
    -> Sort theo source type va Order
    -> Add runtime modifiers
    -> Recalculate derived stats
    -> Clamp resources
```

Khong duoc goi `ClearAllModifiers` neu no xoa ca modifier cua status/combat effect dang song. Phai remove theo source registry.

### Modifier breakdown

UI character sheet phai cho phep xem:

```text
ATK 42
    Base: 20
    Level: +2
    Weapon fixed damage: +10
    Weapon random FireDamage: +4%
    Pet: +5
    Constellation: +5
```

Moi dong can co source, affixId neu co, value, operation va ly do dang active. UI phai phan biet fixed affix va random affix, dong thoi hien thi tier/roll value de debug build va tranh tinh trang stat tang ma khong biet vi sao.
## 11. World runtime va state machine

### Game states

- `Boot`: load save va definitions.
- `Village`: player o safe hub.
- `Exploring`: di chuyen trong map.
- `AutoCombat`: dang co target va action loop.
- `Interaction`: noi chuyen, shop, loot, teleport.
- `TransitioningMap`: load scene/checkpoint.
- `Dead`: player chet, chua cho action moi.
- `Paused`: dung input va realtime systems theo rule.

### Runtime services

```text
WorldFlowController
MapRuntime
PlayerController
AutoCombatController
TargetingSystem
QuestRuntime
NPCInteractionSystem
TeleportSystem
LootSystem
InventoryRuntime
CharacterModifierRuntime
AbilityRunner
CombatSystem
EffectSystemLite
```

`WorldFlowController` dieu phoi state; cac service khong tu y chuyen state cua nhau.

## 12. Save/load

Save file nen luu state, khong luu runtime object:

- Character level, XP, currency.
- Map hien tai va checkpoint.
- Inventory item instances.
- Fixed affix snapshot, random affixes, tier, roll value va rollSeed cua tung item instance.
- Equipped item/pet.
- Constellation nodes.
- Quest state va story flags.
- Teleport/map unlocks.
- Temporary effects neu game cho phep giu qua save.

Khi load:

```text
Load progression
    -> Create AttributeSystem
    -> Apply level/progression
    -> Apply equipment
    -> Restore item affix snapshots, khong roll lai
    -> Apply pet
    -> Apply constellation/passive
    -> Build gameplay modifier context
    -> Spawn player tai checkpoint
```

Rebuild phai idempotent, tranh cong modifier hai lan sau reload scene.

## 13. UX realtime

UI can hien thi:

- HP/MP hien tai va toi da.
- Target hien tai, khoang cach va trang thai combat.
- Auto combat on/off va mode hien tai.
- Ability bar, cooldown, mana cost va cast progress.
- Quest tracker va story objective.
- Minimap, map gate va teleport destination.
- Loot pickup feedback.
- Damage/heal/status floating text.
- Modifier breakdown tu item, pet, constellation va support.

Player phai nhin thay ly do mot action khong the thuc hien: thieu mana, ngoai tam, cooldown, target chet, map locked hay quest chua dat.

## 14. MVP roadmap

### Phase 1: Village va movement

- Player controller.
- Beginner Village scene.
- NPC interaction.
- Mot teleport gate.

### Phase 2: Auto combat

- Monster spawn.
- Target detection.
- Basic attack loop.
- Damage/death pipeline.

### Phase 3: Ability va reward

- Ability bar.
- Cooldown/mana.
- Loot drops.
- XP va level up.

### Phase 4: Quest va story

- Quest definitions.
- NPC dialogue.
- Kill/collect/travel objectives.
- Story flags va map unlock.

### Phase 5: Inventory progression

- Item instances.
- Fixed affixes va random affix roll cho craft/loot.
- Equipment slots.
- Modifier rebuild.
- Pet va constellation foundation.

### Phase 6: Realtime depth

- Support loadout sockets.
- Status/DoT snapshot.
- Projectile/chain/target gameplay modifiers.
- Auto-combat rotation va advanced targeting.

## 15. Quyet dinh thiet ke

1. Gameplay la realtime map RPG, khong phai turn-based card combat.
2. Beginner Village la safe hub va diem bat dau story.
3. Teleport gate la boundary chinh giua cac map.
4. Auto combat chi la mot controller; player van giu quyen di chuyen, target va loadout.
5. `CombatSystem` la authority cho damage va resource resolve.
6. `AbilityDefinition` immutable; `AbilityLoadout` chua support dang link runtime.
7. Support modifier duoc snapshot khi cast; DoT khong doc lai support moi tick.
8. Item, pet, constellation va passive la modifier providers cua character.
9. `AttributeSystem` la authority cho final character attributes.
10. Definition, instance va runtime state phai tach nhau.
11. Quest progress dung event, khong polling scene moi frame.
12. Moi source modifier phai co lifecycle, source va stacking policy ro rang.
13. Random affix dung prefix/suffix slot va unique theo `affixFamilyId`, khong unique theo `affixId` don thuan.
14. MVP item co 1-4 random affix, gom 0-2 prefix va 0-2 suffix; rarity duoc suy ra tu tong so affix.
15. Magic item co dung 1 prefix va 1 suffix; day la invariant cua rarity, khong phai quy uoc UI.

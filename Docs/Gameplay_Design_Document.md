# Gameplay Design Document

## 1. Tổng quan

### Tên tạm thời

**Card Combat - One Character Run**

### Fantasy và trải nghiệm chính

Người chơi điều khiển một nhân vật trong các trận đấu theo lượt. Mỗi lượt, người chơi chỉ được chọn một trong ba lá bài đang có trên tay. Lá bài biến thành hành động trực tiếp lên nhân vật hoặc enemy, sau đó hệ thống tự động resolve combat và chuyển lượt cho enemy.

Trải nghiệm cần đạt được:

- Mỗi lượt có quyết định rõ ràng, nhanh và dễ đọc.
- Bộ bài chỉ có 10 lá nhưng mỗi lá tạo ra một hướng xử lý khác nhau.
- Người chơi luôn phải cân bằng giữa gây sát thương, sống sót, chuẩn bị combo và xử lý tình huống hiện tại.
- Kết quả trận đấu phụ thuộc vào lựa chọn và quản lý tài nguyên, không chỉ phụ thuộc vào chỉ số nhân vật.

### Platform và phạm vi

- Engine: Unity.
- Chế độ đầu tiên: single-player, một player character đấu với một enemy.
- Camera và di chuyển không phải trọng tâm của MVP; combat có thể diễn ra trong arena cố định.
- Không có discard pile phức tạp, energy cost hoặc nhiều nhân vật trong MVP đầu tiên.

## 2. Core loop

```text
Start Battle
    -> Setup player, enemy và deck 10 lá
    -> Shuffle deck
    -> Player Turn: draw 3 lá
    -> Player chọn 1 lá
    -> Validate card
    -> Apply card effect lên target
    -> Resolve animation, damage, heal, status và death check
    -> Nếu chưa có bên chết: Enemy Turn
    -> Enemy chọn action bằng AI
    -> Apply enemy action
    -> Resolve animation, damage, status và death check
    -> Nếu chưa có bên chết: reset/refresh lượt và Player Turn
```

### Luật cốt lõi được chốt cho MVP

1. Deck của player có đúng 10 card instance.
2. Đầu mỗi Player Turn, player rút đúng 3 lá.
3. Player chọn đúng 1 lá. Không chọn được nhiều lá trong cùng một lượt.
4. Sau khi được chọn, lá bài được đưa vào discard pile và không thể xuất hiện lại cho đến khi cần reshuffle.
5. Hai lá còn lại vẫn ở trên tay và được giữ sang lượt sau. Nếu hand đã có 2 lá, lượt sau chỉ rút thêm 1 lá để đủ 3.
6. Khi draw pile không đủ card, discard pile được shuffle thành draw pile.
7. Chỉ kết thúc Player Turn sau khi effect của card đã resolve hoàn toàn.
8. Enemy chỉ được hành động sau khi player action kết thúc và player còn sống.
9. Không cho phép card mới được chọn trong lúc action đang resolve.
10. Trận đấu kết thúc ngay khi một bên có HP bằng 0 hoặc thấp hơn.

### Vì sao giữ lại hai lá chưa chọn

Giữ card giúp người chơi có thông tin và kế hoạch ngắn hạn thay vì hoàn toàn phụ thuộc vào random mỗi lượt. Việc discard card đã dùng vẫn tạo áp lực về timing: dùng lá mạnh sớm có thể khiến deck thiếu công cụ ở cuối trận.

## 3. Trạng thái trận đấu

### Battle states

- `BattleSetup`: tạo combat entities, reset deck và khởi tạo UI.
- `PlayerDraw`: bù hand lên 3 lá.
- `PlayerDecision`: chờ người chơi chọn card.
- `PlayerResolving`: khóa input và resolve card.
- `EnemyThinking`: AI chọn action.
- `EnemyResolving`: khóa input và resolve enemy action.
- `Victory`: player thắng, dừng toàn bộ action mới.
- `Defeat`: player thua, dừng toàn bộ action mới.
- `BattlePaused`: tạm dừng gameplay, không tick cooldown hoặc status nếu game design chưa yêu cầu.

Mọi input card chỉ hợp lệ trong `PlayerDecision`. Mọi transition ra khỏi state resolving phải đi qua death check.

### Thứ tự resolve một action

1. Xác định actor và target.
2. Snapshot các stat cần dùng tại thời điểm cast.
3. Trừ resource hoặc đánh dấu card đã dùng nếu có.
4. Apply immediate effect.
5. Chạy combat pipeline hiện có để tính damage, defense, proc và hit event.
6. Apply status effect và update duration/stack.
7. Chạy animation/VFX/SFX.
8. Hiển thị floating combat text.
9. Kiểm tra HP của cả hai bên.
10. Nếu chưa kết thúc, trả quyền điều khiển về state tiếp theo.

## 4. Deck và hand system

### Cấu trúc deck

Deck là danh sách 10 `CardDefinition` hoặc card instance tham chiếu tới một definition. Trong MVP, deck được tạo sẵn theo từng battle.

```text
Deck: 10 cards
Draw pile: cards chưa rút
Hand: tối đa 3 cards
Discard pile: cards đã dùng
```

### Quy tắc random

- Shuffle bằng Fisher-Yates hoặc API shuffle tương đương.
- Mỗi card instance chỉ xuất hiện một lần trong draw pile.
- Random chỉ quyết định thứ tự draw, không random lại effect sau khi card đã được rút.
- Có seed debug để tái hiện một trận đấu khi test.

### Quy tắc draw

- Đầu Player Turn: `while hand.Count < 3`, draw một card.
- Nếu draw pile hết và discard pile còn card, shuffle discard vào draw pile.
- Nếu cả hai pile đều hết, player vẫn được tiếp tục nếu hand còn card; nếu hand rỗng thì battle lỗi dữ liệu và cần fail-safe.
- MVP không cho player bỏ lượt khi đang có card hợp lệ.

### Card unavailable

Một card có thể không dùng được nếu target không tồn tại, resource không đủ hoặc điều kiện card chưa đạt. Card unavailable vẫn hiển thị nhưng bị disable, kèm lý do ngắn.

Nếu cả 3 card đều unavailable, game phải tự động chọn hành động `Pass` hoặc card fallback cấu hình sẵn. MVP nên dùng `Pass` để tránh soft-lock.

## 5. Card design

Mỗi card cần có các nhóm dữ liệu sau:

- `cardId`: ID ổn định.
- `displayName`, `description`, `icon`.
- `rarity` và `tags` nếu cần cho mở rộng.
- `targetType`: self, enemy hoặc both.
- `effectDefinition`: tham chiếu tới ability/effect hiện có.
- `value`, `duration`, `chance` hoặc các param scale theo stat.
- `isPlayable`: điều kiện dùng card.
- `resolveTime`: thời gian animation dự kiến, không dùng để quyết định damage.

Để tương thích code hiện tại, card nên là lớp game layer gọi `AbilityRunner.UseAndCreateIntent(...)`; damage/status vẫn đi qua `CombatSystem`, `AbilityDefinition` và `EffectSystemLite`. Không copy logic tính damage vào card.

### Bộ 10 card MVP

| # | Tên | Vai trò | Hiệu ứng đề xuất | Mục đích quyết định |
|---|---|---|---|---|
| 1 | Strike | Damage ổn định | Gây 20 damage vật lý | Lá nền, luôn đáng tin cậy |
| 2 | Heavy Blow | Burst | Gây 35 damage, cooldown 1 lượt | Dùng để kết liễu hoặc phá nhịp enemy |
| 3 | Guard | Phòng thủ | Nhận 25 shield trong 1 enemy turn | Dùng khi không thể chịu đòn tiếp theo |
| 4 | Mend | Hồi phục | Hồi 25 HP, giới hạn không vượt max HP | Cứu mạng nhưng làm mất cơ hội gây damage |
| 5 | Poison Dart | Damage theo thời gian | Gây 8 damage và Poison 3 stack/3 lượt | Tốt trước enemy nhiều HP |
| 6 | Weaken | Debuff | Giảm 25% damage enemy trong 2 lượt | Đáp án cho enemy đánh mạnh |
| 7 | Focus | Chuẩn bị | Tăng 30% ATK cho action kế tiếp | Tạo combo với Heavy Blow |
| 8 | Double Edge | High risk/high reward | Gây 45 damage, player mất 10 HP | Kết thúc nhanh khi đang kiểm soát rủi ro |
| 9 | Purify | Utility | Xóa tối đa 2 debuff trên player và hồi 8 HP | Chống build status của enemy |
| 10 | Draw Tactic | Card economy | Bỏ lá này, draw 2 card mới nhưng vẫn chỉ được chọn 1 | Tăng lựa chọn, không tăng số action |

Thông số trên là baseline để prototype, không phải balance cuối cùng. Tất cả giá trị nên được serialize trong data asset để tuning mà không sửa code.

### Nguyên tắc cân bằng

- Một card gây damage trực tiếp nên có giá trị rõ ràng nhưng không vượt quá giá trị của hai lượt thông thường.
- Card phòng thủ cần mạnh đủ để đáng dùng, nhưng không được tạo loop vô hạn.
- Card có effect theo thời gian phải hiển thị tổng damage kỳ vọng và thời lượng.
- Card rút thêm phải tăng chất lượng lựa chọn, không cho phép tạo thêm action trong cùng lượt.
- Deck 10 lá cần có ít nhất: 3 damage, 2 defensive/survival, 2 status/utility và 1 card tạo combo.

## 6. Combat và chỉ số

### Player action

Player card tạo một `AbilityIntent` với actor là player và target là enemy hoặc player tùy card. Intent snapshot các thông số cần thiết ở thời điểm card được chọn.

### Enemy action

Enemy không dùng deck trong MVP. Enemy có một danh sách action/ability và chọn bằng utility score:

- Nếu HP thấp: ưu tiên heal hoặc defense.
- Nếu player có shield: ưu tiên debuff hoặc action phá shield nếu có.
- Nếu có thể kết liễu player: ưu tiên damage lethal.
- Nếu không: chọn action có score cao nhất, tie-break bằng random seed.

Enemy action phải dùng cùng combat pipeline với player để damage và status cho kết quả nhất quán.

### Status timing

Đề xuất timing rõ ràng:

- `OnApply`: chạy ngay khi status được thêm.
- `OnTurnStart`: chạy ở đầu lượt của chủ thể.
- `OnTurnEnd`: chạy ở cuối lượt của chủ thể.
- Hết duration sau khi effect của lượt đó đã chạy.

Ví dụ Poison 3 lượt: Poison tick ở `OnTurnStart` của player bị nhiễm, sau đó giảm duration từ 3 xuống 2.

### Death check

- Sau mỗi damage/heal/resource delta quan trọng đều có thể cập nhật HP, nhưng chỉ transition battle state ở checkpoint resolve.
- Nếu cả hai cùng chết do effect cuối lượt, ưu tiên kết quả theo quy tắc đã cấu hình. MVP dùng `Draw` nếu có thể xảy ra double KO.
- Không cho enemy action bắt đầu nếu player đã chết ở bước trước.

## 7. UX và UI

### Layout battle

- Khu vực trên: enemy portrait, tên, HP bar, status icons, intent/action preview.
- Khu vực giữa: player và enemy, animation action.
- Khu vực dưới: player HP, shield, status icons và hand 3 card.
- Góc bên: turn indicator, battle speed/pause và log rút gọn.

### Card presentation

Mỗi card hiển thị:

- Icon dễ phân biệt silhouette.
- Tên card.
- Cost hoặc cooldown nếu có.
- Mô tả effect bằng số cụ thể.
- Tag: Damage, Defense, Status, Utility.
- Trạng thái selected, hover, unavailable và resolving.

### Flow tương tác

1. Khi Player Turn bắt đầu, hand animate card mới vào vị trí.
2. Hover/focus card hiển thị preview target và kết quả dự kiến.
3. Click card lần đầu để select; click lần hai hoặc nút Confirm để dùng.
4. Trong `PlayerResolving`, card hand bị khóa và card đã dùng bay vào discard.
5. Enemy action được báo trước bằng intent icon hoặc text ngắn nếu enemy có telegraph.
6. Damage/heal/status có floating text và âm thanh riêng.
7. Khi Victory/Defeat, hiển thị kết quả và các nút Replay/Return.

Accessibility tối thiểu: màu không phải tín hiệu duy nhất; card phải có icon/tag/text, input bằng chuột và keyboard/gamepad dùng chung command.

## 8. Kiến trúc triển khai đề xuất

### Data layer

- `CardDefinition : ScriptableObject`: dữ liệu card và tham chiếu `AbilityDefinition`.
- `DeckDefinition : ScriptableObject`: danh sách 10 card definition.
- `EnemyDefinition : ScriptableObject`: stats và danh sách enemy abilities.
- `BattleDefinition : ScriptableObject`: player loadout, enemy và seed/debug settings.

### Runtime layer

- `DeckRuntime`: draw pile, hand, discard pile, shuffle và draw.
- `CardRuntime`: instance runtime của một card đã rút.
- `BattleFlowController`: state machine của battle.
- `PlayerTurnController`: draw, wait selection, validate và submit card.
- `EnemyTurnController`: chọn và submit enemy action.
- `CardResolver`: chuyển card thành ability intent và gọi combat systems.
- `BattleResult`: Victory, Defeat hoặc Draw cùng nguyên nhân.

### Boundary với combat core

`CardResolver` là nơi duy nhất nối card system với combat system. Nó không tự tính damage. Nó gọi `AbilityRunner`, `CombatSystem`, `EffectSystemLite` và chờ event/result để tiếp tục state machine.

### Event tối thiểu

- `BattleStarted`
- `TurnStarted(actor)`
- `CardsDrawn(cards)`
- `CardSelected(card)`
- `CardResolved(card, result)`
- `DamageApplied(source, target, amount)`
- `StatusChanged(target, status)`
- `TurnEnded(actor)`
- `BattleEnded(result)`

## 9. MVP acceptance criteria

Một prototype được xem là đạt khi:

- Có thể start battle với một player và một enemy.
- Deck luôn có đúng 10 card theo `DeckDefinition`.
- Player bắt đầu với 3 card và chỉ chọn được 1 card mỗi lượt.
- Hai card không chọn được giữ lại sang lượt sau.
- Card đã dùng vào discard và có thể quay lại sau khi reshuffle.
- Card damage, heal, shield hoặc status đi qua combat/effect core hiện tại.
- Enemy tự động hành động sau player.
- Input bị khóa trong lúc action resolve.
- HP, status, damage text và turn state được hiển thị chính xác.
- Battle dừng đúng ở Victory, Defeat hoặc Draw.
- Có seed để lặp lại một sequence draw phục vụ debug.

## 10. Test plan

### Unit tests

- Shuffle tạo permutation đủ 10 card, không duplicate.
- Draw bù hand lên đúng 3.
- Chọn card giữ lại hai card chưa dùng.
- Discard được reshuffle khi draw pile hết.
- Không draw duplicate khi chưa discard.
- Card unavailable không được resolve.
- `Draw Tactic` không tạo thêm action trong lượt.
- Card cooldown không thể dùng trước khi hết cooldown.

### Combat tests

- Strike trừ đúng HP sau defense.
- Mend không vượt max HP.
- Poison tick đúng số lượt và hết duration đúng thời điểm.
- Weaken ảnh hưởng đúng damage enemy nhưng không ảnh hưởng player.
- Death check không cho enemy đánh sau khi player chết.
- Double KO trả về kết quả theo rule.

### Play-mode smoke test

Chạy tự động tối thiểu 20 lượt với seed cố định, log deck order, hand, card selected, HP hai bên và battle result. Test phải phát hiện được soft-lock tại `PlayerDecision`, `PlayerResolving` hoặc `EnemyResolving`.

## 11. Milestone triển khai

### Milestone 1 - Combat shell

- Battle scene cố định.
- Player/enemy entities.
- State machine turn cơ bản.
- Một card Strike và enemy basic attack.

### Milestone 2 - Deck loop

- Deck 10 lá, draw/hand/discard/reshuffle.
- UI 3 card.
- Card selection và input lock.

### Milestone 3 - Effect variety

- Implement đủ 10 card MVP.
- Damage, heal, shield, status và cooldown.
- Enemy intent và AI utility cơ bản.

### Milestone 4 - Feel và balance

- Animation/VFX/SFX.
- Combat log và preview.
- Tuning theo telemetry: card pick rate, damage contribution, death turn, win rate.

### Milestone 5 - Content expansion

- Nhiều enemy archetype.
- Deck building ngoài trận.
- Reward và unlock.
- Relic/passive modifier.

## 12. Các điểm chưa thuộc MVP

- Nhiều enemy hoặc nhiều player character.
- Energy/mana cost phức tạp.
- Card upgrade và rarity progression.
- Meta-progression giữa các màn.
- Procedural map.
- Multiplayer.

Các tính năng này chỉ nên thêm sau khi core loop 10 card đã chơi được, dễ hiểu và có balance ổn định.

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace SuspiciousSauna
{
    // Serialized Canvas objects are the source of visual appearance. Only data and responsive
    // container geometry change at runtime; images, fonts, and button styling stay authored.
    [ExecuteAlways]
    public sealed partial class SaunaUI : MonoBehaviour
    {
        public Canvas canvas;
        public GameObject mainMenu, hud, pausePanel, settingsPanel, gameOverPanel;
        public RectTransform playFrame, leftRail, rightRail, starRow, footer, overlay;
        public ScrollRect receiptsScroll, statusScroll, menuButtonsScroll, settingsScroll;
        public RectTransform menuLeft, menuArt, pauseCard, settingsCard, gameOverCard;
        public RectTransform pauseContent, settingsContent, gameOverContent;
        public Text receiptHeading, emptyOrders, ratingText, waitingText, completedText, failedText, toolText, footerText;
        public Text bgmText, sfxText, scaleHint, gameOverText;
        public Image[] starFills;
        public Button startButton, settingsButton, quitButton, resumeButton, pauseSettingsButton, menuButton;
        public Button settingsBackButton, restartButton, gameOverMenuButton;
        public Slider musicSlider, effectsSlider;
        public Dropdown uiScaleDropdown;
        public SaunaReceiptView receiptPrefab;
        public GameObject receiptPreview;
        public SaunaBubbleView[] bubbles;
        public RectTransform interactionHint;
        public Text interactionText;
        public Image cleaningProgress;
        public SaunaCookingView cooking;
        public CanvasGroup narration;
        public Text narrationText, heldItemsText;
        public readonly List<Receipt> Receipts = new List<Receipt>();
        SaunaGame game;
        ScreenState previousState = (ScreenState)(-1);
        int lastScale = -1, lastWidth, lastHeight;
        readonly List<Vector2> edgeBubbles = new List<Vector2>();
        public void Initialize(SaunaGame controller)
        {
            game = controller;
            game.SessionStarted += ResetReceipts; game.OrderAccepted += ShowReceipt; game.OrderResolved += HideReceipt;
            startButton.onClick.AddListener(game.StartGame); settingsButton.onClick.AddListener(game.OpenSettings);
            quitButton.onClick.AddListener(game.Quit); resumeButton.onClick.AddListener(game.Resume);
            pauseSettingsButton.onClick.AddListener(game.OpenSettings); menuButton.onClick.AddListener(game.MainMenu);
            settingsBackButton.onClick.AddListener(game.CloseSettings); restartButton.onClick.AddListener(game.StartGame);
            gameOverMenuButton.onClick.AddListener(game.MainMenu);
            musicSlider.onValueChanged.AddListener(v => game.Sound.SetMusic(v));
            effectsSlider.onValueChanged.AddListener(v => game.Sound.SetEffects(v));
            uiScaleDropdown.onValueChanged.AddListener(i => game.SetUiScale(i + 1));
            if (receiptPreview != null) receiptPreview.SetActive(false);
            foreach (var bubble in bubbles) bubble.gameObject.SetActive(false);
            interactionHint.gameObject.SetActive(false);
            if (cooking != null) cooking.Initialize(game);
            InitializeRetail();
        }
        void OnDestroy()
        {
            if (game == null) return;
            game.SessionStarted -= ResetReceipts; game.OrderAccepted -= ShowReceipt; game.OrderResolved -= HideReceipt;
            game.RetailAccepted -= ShowRetailReceipt; game.RetailResolved -= HideRetailReceipt;
        }
        void ResetReceipts()
        {
            foreach (var receipt in Receipts) if (receipt.view != null) Destroy(receipt.view.gameObject);
            Receipts.Clear(); receiptsScroll.verticalNormalizedPosition = 1;
        }
        void ShowReceipt(SaunaTable table, QuestEntry quest)
        {
            var view = Instantiate(receiptPrefab, receiptsScroll.content);
            view.name = "Receipt_Table_" + (table.index + 1).ToString("00");
            var receipt = new Receipt { table = table, view = view };
            Receipts.Add(receipt);
            view.Clicked = () => { Receipts.Remove(receipt); Receipts.Insert(0, receipt); receiptsScroll.verticalNormalizedPosition = 1; };
        }
        void HideReceipt(SaunaTable table, bool success)
        {
            foreach (var receipt in Receipts) if (receipt.table == table) receipt.closing = true;
            receiptsScroll.StopMovement();
            receiptsScroll.verticalNormalizedPosition = 1;
        }
        void LateUpdate()
        {
            if (!Application.isPlaying)
            {
                if (canvas != null && menuLeft != null && settingsContent != null && gameOverContent != null && statusScroll != null) Layout();
                return;
            }
            if (game == null) return;
            bool changed = previousState != game.State || lastScale != game.UiScale || lastWidth != Screen.width || lastHeight != Screen.height;
            bool menu = game.State == ScreenState.Menu || (game.State == ScreenState.Settings && game.SettingsReturn == ScreenState.Menu);
            mainMenu.SetActive(menu); hud.SetActive(!menu);
            pausePanel.SetActive(game.State == ScreenState.Paused);
            settingsPanel.SetActive(game.State == ScreenState.Settings);
            gameOverPanel.SetActive(game.State == ScreenState.GameOver);
            if (cooking != null) cooking.gameObject.SetActive(game.State == ScreenState.Cooking);
            if (retailPicker != null) retailPicker.gameObject.SetActive(game.State == ScreenState.RetailPicking);
            if (changed)
            {
                Layout(); previousState = game.State; lastScale = game.UiScale; lastWidth = Screen.width; lastHeight = Screen.height;
                musicSlider.SetValueWithoutNotify(game.Sound.Music); effectsSlider.SetValueWithoutNotify(game.Sound.Effects);
                uiScaleDropdown.SetValueWithoutNotify(game.UiScale - 1);
            }
            uiScaleDropdown.interactable = !game.InSession;
            scaleHint.text = game.InSession ? "UI 크기는 메인메뉴에서 변경할 수 있어요." : "100%, 200%, 300% 중 선택하세요.";
            bgmText.text = "배경음  " + Mathf.RoundToInt(game.Sound.Music * 100) + "%";
            sfxText.text = "효과음  " + Mathf.RoundToInt(game.Sound.Effects * 100) + "%";
            receiptHeading.text = "받은 주문  " + Receipts.Count(r => !r.closing).ToString("00");
            emptyOrders.gameObject.SetActive(Receipts.Count == 0);
            ratingText.text = game.Rating.ToString("0.00");
            for (int i = 0; i < starFills.Length; i++) starFills[i].fillAmount = Mathf.Clamp01(game.Rating - i);
            waitingText.text = game.Tables.Count(t => t.service == TableServiceState.Waiting || t.service == TableServiceState.Arriving).ToString();
            completedText.text = game.Completed.ToString(); failedText.text = game.Failed.ToString();
            toolText.text = game.HasCleaningTool ? "청소 도구를 들고 있어요" : "청소 도구는 카운터 오른쪽";
            if (heldItemsText != null) heldItemsText.text = "오른손  " + (game.Model.inventory.Right != null ? game.Model.inventory.Right.displayName : "비어 있음") + "\n왼손  " + (game.Model.inventory.Left != null ? game.Model.inventory.Left.displayName : "비어 있음");
            PresentRetailHUD();
            footerText.text = game.ToastTime > 0 ? game.Toast : "WASD / 방향키  이동     F  상호작용     ESC  일시정지";
            if (narration != null) { narration.alpha = Mathf.Clamp01(game.ToastTime / .75f) * Mathf.Clamp01((4.5f - game.ToastTime) / .15f); narrationText.text = game.Toast; }
            gameOverText.text = "별점 1.0  ·  응대 완료 " + game.Completed + "건";
            UpdateReceipts(); UpdateIndicators(); UpdateRetailIndicator(); UpdateHint();
        }
        public static void Box(RectTransform r, float x, float y, float width, float height)
        {
            r.anchorMin = r.anchorMax = new Vector2(0, 1); r.pivot = new Vector2(0, 1);
            r.anchoredPosition = new Vector2(x, -y); r.sizeDelta = new Vector2(width, height);
        }
        float Width => Screen.width / canvas.scaleFactor;
        float Height => Screen.height / canvas.scaleFactor;
        Rect Play => SaunaMap.PlayRect(Width, Height);
        void Layout()
        {
            Rect p = Play; float w = Width, h = Height, u = game != null ? game.UiScale : 1;
            Box(playFrame, p.x - 2, p.y - 2, p.width + 4, p.height + 4);
            Box(overlay, 0, 0, w, h);
            bool side = p.x >= 125;
            Box(leftRail, side ? 12 : 6, side ? p.y : 4, side ? p.x - 32 : w - 12, side ? h - p.y - 35 : Mathf.Max(42, p.y - 43));
            Box(rightRail, p.xMax + 20, p.y + 15, Mathf.Max(1, p.x - 35), p.height - 30); rightRail.gameObject.SetActive(side);
            Box(starRow, p.center.x - 112, side ? 0 : Mathf.Max(0, p.y - 40), 224, 38);
            Box(footer, p.x + 4, p.yMax + 4, p.width - 8, h - p.yMax - 8);
            float statusScale = Mathf.Min(u, Mathf.Max(.6f, (p.x - 45) / 230));
            statusScroll.content.localScale = Vector3.one * statusScale; statusScroll.content.sizeDelta = new Vector2(Mathf.Max(230, (p.x - 45) / statusScale), 530); statusScroll.horizontal = false;
            float fit = Mathf.Min(1, h / 760f);
            float mw = w > h * 1.15f ? Mathf.Min(420 * fit, w * .36f) : w - 64;
            menuLeft.localScale = Vector3.one * fit;
            Box(menuLeft, w > h * 1.15f ? w * .1f : 32, h * .1f, mw / fit, h * .82f / fit);
            float art = Mathf.Min(w * .39f, h * .68f);
            Box(menuArt, w * .55f, (h - art) * .5f, art, art); menuArt.gameObject.SetActive(w > h * 1.15f);
            menuButtonsScroll.content.localScale = Vector3.one * u;
            menuButtonsScroll.content.sizeDelta = new Vector2(Mathf.Max(100, (mw / fit - 20) / u), 214);
            PlaceModal(pauseCard, pauseContent, 480, 370, u);
            PlaceModal(settingsCard, null, 600, 510, u);
            PlaceModal(gameOverCard, gameOverContent, 520, 380, u);
            var settingsTitle = settingsCard.Find("Title") as RectTransform;
            if (settingsTitle != null) Box(settingsTitle, 20, 20, settingsCard.rect.width - 40, 45);
            settingsContent.localScale = Vector3.one * u;
            settingsContent.sizeDelta = new Vector2(Mathf.Max(160, (settingsCard.rect.width - 80) / u), 360);
            receiptsScroll.horizontal = !side; receiptsScroll.vertical = side;
            if (narration != null) Box((RectTransform)narration.transform, p.x + 14, p.y + 12, p.width - 28, 48);
            if (cooking != null)
            {
                PlaceModal(cooking.card, null, 650, 570, u);
                int columns = Mathf.Clamp(Mathf.FloorToInt((cooking.card.rect.width - 56) / (180 * u)), 1, 3);
                cooking.content.localScale = Vector3.one * u;
                cooking.content.sizeDelta = new Vector2(columns * 180, Mathf.CeilToInt(cooking.foods.Length / (float)columns) * 120);
                for (int i = 0; i < cooking.foods.Length; i++) Box((RectTransform)cooking.foods[i].transform, (i % columns) * 180, (i / columns) * 120, 164, 108);
                cooking.scroll.horizontal = false;
            }
            LayoutRetail(u);
        }
        void PlaceModal(RectTransform card, RectTransform content, float width, float height, float scale)
        {
            float cw = Mathf.Min(width * scale, Width - 32), ch = Mathf.Min(height * scale, Height - 32);
            Box(card, (Width - cw) * .5f, (Height - ch) * .5f, cw, ch);
            if (content != null)
            {
                float s = Mathf.Min(scale, Mathf.Min(cw / width, ch / height));
                content.localScale = Vector3.one * s;
                Box(content, (cw - width * s) * .5f, (ch - height * s) * .5f, width, height);
            }
        }
        void UpdateReceipts()
        {
            float u = game.UiScale;
            bool side = Play.x >= 125;
            float viewWidth = receiptsScroll.viewport.rect.width, viewHeight = receiptsScroll.viewport.rect.height;
            bool compact = !side;
            // A fixed square viewport leaves a finite rail. Cap the requested HUD scale so
            // each receipt's countdown stays visible; modal menus retain the selected scale.
            float factor = compact ? 1 : Mathf.Min(u, Mathf.Max(.6f, (viewWidth - 12) / 250));
            // Shrink the authored paper bounds, not the text scale. Half-height spacing
            // stays constant even when orders extend beyond the bottom of the viewport.
            float cw = side ? Mathf.Min(viewWidth - 12, receiptPrefab.rect.rect.width * factor) : 380 * u;
            float ch = compact ? Mathf.Max(28, viewHeight - 8) : receiptPrefab.rect.rect.height * factor;
            float step = ch * .5f;
            receiptsScroll.content.sizeDelta = new Vector2(side ? cw : Mathf.Max(viewWidth, Receipts.Count * (cw + 12)), side ? Mathf.Max(viewHeight, ch + Mathf.Max(0, Receipts.Count - 1) * step) : viewHeight);
            for (int i = Receipts.Count - 1; i >= 0; i--)
            {
                var r = Receipts[i];
                r.slide = Mathf.MoveTowards(r.slide, r.closing ? 0 : 1, Time.unscaledDeltaTime * 2.8f);
                if (r.closing && r.slide <= 0) { Destroy(r.view.gameObject); Receipts.RemoveAt(i); continue; }
                float y = side ? i * step : 0;
                r.y = Mathf.Lerp(r.y, y, 1 - Mathf.Exp(-Time.unscaledDeltaTime * 12));
                float animatedY = Mathf.Lerp(-Height - ch, r.y, Mathf.SmoothStep(0, 1, r.slide));
                r.view.rect.localScale = Vector3.one * factor;
                Box(r.view.rect, side ? Mathf.Min(i * 2, 12) : i * (cw + 12), animatedY, cw / factor, ch / factor);
                r.view.transform.SetSiblingIndex(Receipts.Count - 1 - i);
                if (r.retailOrder != null) r.view.Present(r.retailOrder, r.closing, compact);
                else r.view.Present(r.table, r.closing, compact);
            }
        }
        Vector2 Point(Vector2 world)
        {
            var p = game.WorldCamera.WorldToScreenPoint(world); return new Vector2(p.x, Screen.height - p.y) / canvas.scaleFactor;
        }
        void UpdateIndicators()
        {
            Rect p = Play; edgeBubbles.Clear();
            for (int i = 0; i < bubbles.Length; i++)
            {
                if (i >= game.Tables.Count) { bubbles[i].gameObject.SetActive(false); continue; }
                var t = game.Tables[i]; var bubble = bubbles[i];
                bool active = t.service == TableServiceState.Arriving || t.service == TableServiceState.Waiting || t.service == TableServiceState.Ordered;
                bubble.gameObject.SetActive(active); if (!active) continue;
                var g = game.Guests.Where(c => c.table == i && c.state != GuestState.Leaving).OrderBy(c => p.Contains(Point(c.position)) ? 0 : 1).ThenBy(c => c.id).FirstOrDefault();
                if (g == null) { bubble.gameObject.SetActive(false); continue; }
                Vector2 raw = Point(g.position + Vector2.up * (g.state == GuestState.Seated ? 1.95f : 1.3f)), pos = raw;
                float size = Mathf.Min(34 * game.UiScale, 48, p.width * .095f), pad = size * .7f;
                Rect safe = new Rect(p.x + pad, p.y + pad, p.width - pad * 2, p.height - pad * 2);
                bool inside = safe.Contains(pos);
                if (!inside)
                {
                    var d = raw - p.center;
                    float r = Mathf.Min(safe.width * .5f / Mathf.Max(.001f, Mathf.Abs(d.x)), safe.height * .5f / Mathf.Max(.001f, Mathf.Abs(d.y)));
                    pos = p.center + d * r;
                    if (edgeBubbles.Any(b => Vector2.Distance(b, pos) < size * 1.1f)) { bubble.gameObject.SetActive(false); continue; }
                    edgeBubbles.Add(pos);
                    bubble.direction.text = Mathf.Abs(d.x) > Mathf.Abs(d.y) ? (d.x > 0 ? "›" : "‹") : (d.y > 0 ? "↓" : "↑");
                }
                else bubble.direction.text = "";
                Box(bubble.rect, pos.x - size * .5f, pos.y - size * .5f, size, size);
                bubble.label.text = t.service == TableServiceState.Ordered ? t.quest.id : "!";
            }
        }
        void UpdateHint()
        {
            string hint = "";
            if (game.State != ScreenState.Playing) { interactionHint.gameObject.SetActive(false); return; }
            if (game.TargetRetailShelf) hint = "F  잡화 담기";
            else if (game.TargetWasteBin) hint = game.WasteConfirmed ? "F  한 번 더 눌러 전부 버리기" : "F  음식물 버리기";
            else if (game.TargetOrderTable != null) hint = game.TargetOrderTable.service == TableServiceState.Waiting ? "F  테이블 주문 받기" : game.TargetOrderTable.IsFoodOrder ? "F  주문한 음식 전달" : "F  " + game.TargetOrderTable.quest.id + " 응대 완료";
            else if (game.TargetStation >= 0) hint = game.TargetStation == 1 ? (game.retailLayout.AtStaffSide(game.PlayerPosition) ? (game.ActiveRetailOrder != null ? "F  잡화 전부 전달" : "F  잡화 주문 받기") : "카운터 뒤에서 응대") : "F  " + (game.TargetStation == 3 && game.HasCleaningTool ? "청소 도구 내려놓기" : SaunaMap.StationNames[game.TargetStation]);
            else if (game.TargetTable != null)
            {
                var t = game.TargetTable;
                hint = !game.HasCleaningTool ? "청소 도구가 필요해요" : t.cleaningMode == 0 ? "F  3초 동안 누르기" : t.cleaningMode == 1 ? "F  연타  " + t.presses + " / " + game.tuning.cleaningPresses : "F  청소 시작";
            }
            interactionHint.gameObject.SetActive(hint.Length > 0);
            if (hint.Length == 0) return;
            var p = Play; var pos = Point(game.PlayerPosition + Vector2.down * .65f);
            float width = Mathf.Min(240 * game.UiScale, p.width - 16), height = Mathf.Min(36 * game.UiScale, 64);
            Box(interactionHint, Mathf.Clamp(pos.x - width * .5f, p.x + 8, p.xMax - width - 8), Mathf.Clamp(pos.y + 10, p.y + 8, p.yMax - height - 8), width, height);
            interactionText.text = hint;
            var table = game.TargetTable;
            cleaningProgress.fillAmount = table == null || table.cleaningMode < 0 ? 0 : table.cleaningMode == 0 ? table.held / game.tuning.cleaningHoldSeconds : table.presses / (float)game.tuning.cleaningPresses;
        }
        [ContextMenu("편집 미리보기/플레이 화면")]
        public void PreviewPlay() { mainMenu.SetActive(false); hud.SetActive(true); pausePanel.SetActive(false); settingsPanel.SetActive(false); gameOverPanel.SetActive(false); if (cooking != null) cooking.gameObject.SetActive(false); if (narration != null) narration.alpha = 0; }
        [ContextMenu("편집 미리보기/메인메뉴")]
        public void PreviewMenu() { PreviewPlay(); mainMenu.SetActive(true); hud.SetActive(false); }
    }
}

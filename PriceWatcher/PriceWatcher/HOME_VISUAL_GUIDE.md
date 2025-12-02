# Home Page Redesign - Visual Guide

## 🎨 Layout Overview

```
┌─────────────────────────────────────────────────────────────────┐
│                         HEADER (Existing)                        │
│                    Logo | Search | Cart | User                  │
└─────────────────────────────────────────────────────────────────┘

┌──────────┬──────────────────────────────────────────────────────┐
│          │                                                       │
│ SIDEBAR  │                  HERO BANNER                         │
│          │  ┌─────────────────────────────────────────────┐    │
│ 📱 Phone │  │ 🌟 New Feature                              │    │
│ 💻 Laptop│  │                                             │    │
│ 📷 Camera│  │ Tìm Kiếm Bằng Hình Ảnh                     │    │
│ 🎧 Audio │  │                                             │    │
│ ⌚ Watch  │  │ [Tìm kiếm ngay] [Tìm kiếm văn bản]        │    │
│ 🏠 Home  │  │                                             │    │
│ 📚 Books │  │ 2.5M+ Products | 3 Platforms | 24/7        │    │
│ 👕 Fashion│  └─────────────────────────────────────────────┘    │
│          │                                                       │
│ [View All]│              QUICK ACTION CARDS                      │
│          │  ┌──────┐ ┌──────┐ ┌──────┐ ┌──────┐              │
│          │  │ 🔍   │ │ 📷   │ │ 📂   │ │ 🛒   │              │
│          │  │Search│ │Visual│ │Categ.│ │ Cart │              │
│          │  └──────┘ └──────┘ └──────┘ └──────┘              │
│          │                                                       │
│          │              SEARCH HISTORY                           │
│          │  ┌─────────────────────────────────────────────┐    │
│          │  │ 🕐 Recent Searches                          │    │
│          │  └─────────────────────────────────────────────┘    │
│          │                                                       │
│          │              TRACK PRODUCT                            │
│          │  ┌─────────────────────────────────────────────┐    │
│          │  │ [Paste Tiki URL] [Track Now]               │    │
│          │  └─────────────────────────────────────────────┘    │
│          │                                                       │
│          │              FLASH DEALS                              │
│          │  ⚡ Flash Sale | Ends in: 02:30:45                   │
│          │  ┌────┐ ┌────┐ ┌────┐ ┌────┐ ┌────┐              │
│          │  │Prod│ │Prod│ │Prod│ │Prod│ │Prod│              │
│          │  └────┘ └────┘ └────┘ └────┘ └────┘              │
│          │                                                       │
│          │              SUGGESTED PRODUCTS                       │
│          │  ⭐ Gợi Ý Hôm Nay                                    │
│          │  [Personal] [All] [Tiki] [Shopee]                   │
│          │  ┌────┐ ┌────┐ ┌────┐ ┌────┐ ┌────┐ ┌────┐      │
│          │  │Prod│ │Prod│ │Prod│ │Prod│ │Prod│ │Prod│      │
│          │  └────┘ └────┘ └────┘ └────┘ └────┘ └────┘      │
│          │                                                       │
└──────────┴──────────────────────────────────────────────────────┘
```

## 🎯 Component Breakdown

### 1. Sidebar Categories (Left)
```
┌─────────────────────────┐
│ 📂 Danh Mục Sản Phẩm   │ ← Gradient header
├─────────────────────────┤
│ 📱 Điện Thoại    2.5k+ →│ ← Hover: slide right
│ 💻 Laptop & IT   1.8k+ →│
│ 📷 Máy Ảnh       890+  →│
│ 🎧 Âm Thanh      1.2k+ →│
│ ⌚ Đồng Hồ       650+  →│
│ 🏠 Nhà Cửa       3.1k+ →│
│ 📚 Sách          5.4k+ →│
│ 👕 Thời Trang    4.2k+ →│
├─────────────────────────┤
│ 📂 Xem tất cả danh mục  │ ← Button
└─────────────────────────┘
```

**Features:**
- Blue gradient header
- 48px icon containers
- Product counts
- Hover: left border + slide
- Sticky positioning

### 2. Hero Banner
```
┌────────────────────────────────────────────────────────┐
│ 🌟 Tính năng mới                                       │
│                                                         │
│ Tìm Kiếm Bằng Hình Ảnh                                │
│                                                         │
│ Chụp hoặc tải lên hình ảnh sản phẩm, chúng tôi sẽ    │
│ tìm giá tốt nhất từ Shopee, Tiki, Lazada              │
│                                                         │
│ [📷 Tìm kiếm ngay]  [🔍 Tìm kiếm văn bản]            │
│                                                         │
│ 2.5M+ Products | 3 Platforms | 24/7 Updates           │
│                                                         │
│                                    [Floating Icons] →  │
└────────────────────────────────────────────────────────┘
```

**Colors:**
- Background: Purple gradient (#667eea → #764ba2)
- Primary CTA: White background
- Secondary CTA: Transparent with border
- Text: White

**Animations:**
- Content slides in from left
- Floating product icons
- Pulsing background circles

### 3. Quick Action Cards
```
┌──────────────┐ ┌──────────────┐ ┌──────────────┐ ┌──────────────┐
│ 🔍 (Blue)    │ │ 📷 (Purple)  │ │ 📂 (Pink)    │ │ 🛒 (Orange)  │
│              │ │              │ │              │ │              │
│ Tìm Kiếm     │ │ Tìm Bằng     │ │ Danh Mục     │ │ Giỏ Hàng     │
│ Đa Sàn       │ │ Hình Ảnh     │ │              │ │              │
│              │ │              │ │              │ │              │
│ So sánh giá  │ │ Chụp ảnh để  │ │ Khám phá theo│ │ Xem sản phẩm │
│ trên nhiều   │ │ tìm sản phẩm │ │ danh mục     │ │ đã lưu       │
│ sàn       →  │ │           →  │ │           →  │ │           →  │
└──────────────┘ └──────────────┘ └──────────────┘ └──────────────┘
```

**Hover Effects:**
- Lift up 8px
- Shadow increases
- Icon rotates -5deg
- Arrow slides right
- Gradient overlay appears

### 4. Platform Tabs (Enhanced)
```
┌────────────────────────────────────────────────────┐
│ [Personal] [All] [Tiki] [Shopee]                  │
│    ━━━━━                                           │
│    Active tab has gradient background              │
└────────────────────────────────────────────────────┘
```

**States:**
- **Normal:** White background, gray text
- **Hover:** Blue border, blue text, lift up
- **Active:** Blue gradient, white text, shadow

## 🎨 Color System

### Primary Colors
```css
Blue:    #1a94ff → #0d6efd  (Primary actions)
Purple:  #667eea → #764ba2  (Hero, Visual search)
Pink:    #f093fb → #f5576c  (Categories)
Orange:  #fa709a → #fee140  (Cart, Deals)
Green:   #00ab56            (Success states)
Red:     #ff424e            (Deals, Discounts)
```

### Neutral Colors
```css
White:   #ffffff
Light:   #f8f9fa
Gray:    #f0f0f0
Text:    #333333
Muted:   #999999
```

## 📐 Spacing System

```css
Gap Small:    0.5rem  (8px)
Gap Medium:   1rem    (16px)
Gap Large:    1.5rem  (24px)
Gap XL:       2rem    (32px)

Padding Card: 1.5rem  (24px)
Padding Hero: 3rem    (48px)

Border Radius:
  Small:  8px
  Medium: 12px
  Large:  16px
  XL:     24px
```

## 🎭 Animation Timings

```css
Fast:     0.2s ease
Base:     0.3s ease
Slow:     0.6s ease

Hover:    transform 0.3s ease
Slide:    0.6s cubic-bezier(0.4, 0, 0.2, 1)
Float:    3s ease-in-out infinite
Pulse:    4s ease-in-out infinite
```

## 📱 Responsive Breakpoints

### Desktop (1200px+)
- Sidebar visible
- Hero with visuals
- 4-column quick actions
- 6-column products

### Tablet (768px - 1199px)
- Sidebar hidden
- Hero stacked
- 2-column quick actions
- 4-column products

### Mobile (< 768px)
- Compact hero
- 1-column quick actions
- Horizontal cards
- 2-column products

## 🎯 Interactive States

### Hover States
```
Category Link:
  Normal:  White background
  Hover:   Blue tint, slide right, left border

Quick Action:
  Normal:  White, subtle shadow
  Hover:   Lift 8px, stronger shadow, icon rotate

Platform Tab:
  Normal:  White, gray text
  Hover:   Blue border, lift 2px
  Active:  Blue gradient, white text
```

### Focus States
```
All interactive elements:
  - 3px blue outline
  - 2px offset
  - Visible keyboard navigation
```

## 🌟 Key Visual Features

### 1. Gradient Backgrounds
- Hero: Purple gradient
- Buttons: Blue gradient
- Icons: Various gradients
- Hover overlays: Subtle gradients

### 2. Shadows
```css
Small:  0 2px 8px rgba(0,0,0,0.06)
Medium: 0 8px 24px rgba(26,148,255,0.15)
Large:  0 12px 24px rgba(0,0,0,0.12)
Hover:  0 20px 60px rgba(102,126,234,0.4)
```

### 3. Animations
- **Slide In:** Content enters from left
- **Float:** Icons move up and down
- **Pulse:** Circles expand and contract
- **Lift:** Cards rise on hover
- **Rotate:** Icons tilt on hover
- **Slide:** Arrows move on hover

## 💡 Design Patterns

### Card Pattern
```
┌─────────────────┐
│ [Icon]          │ ← Gradient background
│                 │
│ Title           │ ← Bold, dark text
│ Description     │ ← Light, gray text
│              → │ ← Arrow indicator
└─────────────────┘
```

### List Item Pattern
```
┌─────────────────────────┐
│ [Icon] Title      Count →│
│        Subtitle          │
└─────────────────────────┘
```

### Button Pattern
```
┌──────────────────┐
│ [Icon] Text      │ ← Gradient or solid
└──────────────────┘
```

## 🎨 Typography

```css
Hero Title:     3rem, 800 weight
Section Title:  1.5rem, 800 weight
Card Title:     1rem, 700 weight
Body Text:      0.95rem, 400 weight
Small Text:     0.85rem, 400 weight
```

## ✨ Special Effects

### Glassmorphism
```css
background: rgba(255, 255, 255, 0.2);
backdrop-filter: blur(10px);
border: 1px solid rgba(255, 255, 255, 0.3);
```

### Gradient Overlay
```css
background: linear-gradient(
  135deg,
  rgba(26, 148, 255, 0.05) 0%,
  transparent 100%
);
```

### Shine Effect
```css
background: linear-gradient(
  90deg,
  transparent,
  rgba(255, 255, 255, 0.3),
  transparent
);
animation: shine 3s infinite;
```

---

## 🎉 Result

A modern, vibrant, Tiki-inspired home page with:
- ✨ Eye-catching hero banner
- 🎨 Colorful gradient system
- 🎭 Smooth animations
- 📱 Fully responsive
- ♿ Accessible design
- ⚡ Performance optimized

**Your home page is now ready to impress!** 🚀

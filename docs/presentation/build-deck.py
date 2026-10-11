"""Builds the CTO-facing overview deck for the QA automation framework.

Run:  python docs/presentation/build-deck.py
Out:  docs/presentation/QAAutomation-Framework-Overview.pptx

Deliberately plain Python: one helper per visual idea, then one function per
slide, in slide order. Edit the slide function, re-run, re-open.
"""

import os

from pptx import Presentation
from pptx.dml.color import RGBColor
from pptx.enum.shapes import MSO_SHAPE
from pptx.enum.text import MSO_ANCHOR, PP_ALIGN
from pptx.util import Inches, Pt

# --------------------------------------------------------------------------
# Palette and type
# --------------------------------------------------------------------------

NAVY = RGBColor(0x0F, 0x27, 0x42)
NAVY_SOFT = RGBColor(0x1B, 0x3A, 0x5C)
TEAL = RGBColor(0x00, 0xA8, 0xA0)
AMBER = RGBColor(0xE8, 0x9C, 0x24)
CORAL = RGBColor(0xD9, 0x5A, 0x53)
VIOLET = RGBColor(0x6C, 0x5C, 0xE7)
INK = RGBColor(0x1F, 0x29, 0x33)
GREY = RGBColor(0x5E, 0x6B, 0x7A)
GREY_SOFT = RGBColor(0x8A, 0x97, 0xA4)
WASH = RGBColor(0xF5, 0xF7, 0xF9)
CODE_BG = RGBColor(0x10, 0x2B, 0x46)
WHITE = RGBColor(0xFF, 0xFF, 0xFF)
BORDER = RGBColor(0xDA, 0xE1, 0xE8)

BODY_FONT = "Segoe UI"
MONO_FONT = "Consolas"

SLIDE_W = 13.333
SLIDE_H = 7.5
MARGIN = 0.75
CONTENT_W = SLIDE_W - (MARGIN * 2)


# --------------------------------------------------------------------------
# Small helpers
# --------------------------------------------------------------------------


def new_slide(prs, background=WHITE):
    slide = prs.slides.add_slide(prs.slide_layouts[6])
    slide.background.fill.solid()
    slide.background.fill.fore_color.rgb = background
    return slide


def textbox(
    slide,
    left,
    top,
    width,
    height,
    text,
    size=13,
    bold=False,
    color=INK,
    align=PP_ALIGN.LEFT,
    font=BODY_FONT,
    anchor=MSO_ANCHOR.TOP,
    line_spacing=1.2,
    space_after=4,
):
    """One textbox. Newlines in `text` become separate paragraphs."""
    box = slide.shapes.add_textbox(
        Inches(left), Inches(top), Inches(width), Inches(height)
    )
    frame = box.text_frame
    frame.word_wrap = True
    frame.margin_left = 0
    frame.margin_right = 0
    frame.margin_top = 0
    frame.margin_bottom = 0
    frame.vertical_anchor = anchor

    lines = text.split("\n")
    index = 0
    for line in lines:
        if index == 0:
            paragraph = frame.paragraphs[0]
        else:
            paragraph = frame.add_paragraph()
        paragraph.alignment = align
        paragraph.line_spacing = line_spacing
        paragraph.space_after = Pt(space_after)
        run = paragraph.add_run()
        run.text = line
        run.font.size = Pt(size)
        run.font.bold = bold
        run.font.color.rgb = color
        run.font.name = font
        index = index + 1
    return box


def shape(
    slide,
    left,
    top,
    width,
    height,
    fill=WHITE,
    line=None,
    line_width=1.0,
    kind=MSO_SHAPE.ROUNDED_RECTANGLE,
    radius=0.08,
):
    item = slide.shapes.add_shape(
        kind, Inches(left), Inches(top), Inches(width), Inches(height)
    )
    item.shadow.inherit = False

    if fill is None:
        item.fill.background()
    else:
        item.fill.solid()
        item.fill.fore_color.rgb = fill

    if line is None:
        item.line.fill.background()
    else:
        item.line.color.rgb = line
        item.line.width = Pt(line_width)

    if kind == MSO_SHAPE.ROUNDED_RECTANGLE:
        item.adjustments[0] = radius

    item.text_frame.word_wrap = True
    return item


def shape_label(item, text, size=13, bold=False, color=INK, font=BODY_FONT):
    """Centre a single line of text inside an auto-shape."""
    frame = item.text_frame
    frame.margin_left = Inches(0.08)
    frame.margin_right = Inches(0.08)
    frame.margin_top = Inches(0.04)
    frame.margin_bottom = Inches(0.04)
    frame.vertical_anchor = MSO_ANCHOR.MIDDLE

    lines = text.split("\n")
    index = 0
    for line in lines:
        if index == 0:
            paragraph = frame.paragraphs[0]
        else:
            paragraph = frame.add_paragraph()
        paragraph.alignment = PP_ALIGN.CENTER
        paragraph.line_spacing = 1.1
        run = paragraph.add_run()
        run.text = line
        run.font.size = Pt(size)
        run.font.bold = bold
        run.font.color.rgb = color
        run.font.name = font
        index = index + 1


def header(slide, kicker, title, number=None):
    shape(slide, 0, 0, SLIDE_W, 0.1, fill=TEAL, kind=MSO_SHAPE.RECTANGLE)
    if kicker:
        textbox(
            slide, MARGIN, 0.52, CONTENT_W, 0.28, kicker.upper(), size=10.5, bold=True, color=TEAL
        )
    textbox(slide, MARGIN, 0.86, CONTENT_W, 0.62, title, size=29, bold=True, color=NAVY)
    if number is not None:
        textbox(
            slide,
            SLIDE_W - MARGIN - 1.0,
            7.0,
            1.0,
            0.3,
            str(number),
            size=10,
            color=GREY_SOFT,
            align=PP_ALIGN.RIGHT,
        )


def card(
    slide,
    left,
    top,
    width,
    height,
    title,
    body,
    accent=TEAL,
    badge=None,
    dot_top=0.28,
    body_top=1.0,
):
    shape(slide, left, top, width, height, fill=WHITE, line=BORDER)
    dot = shape(
        slide, left + 0.3, top + dot_top, 0.42, 0.42, fill=accent, kind=MSO_SHAPE.OVAL
    )
    if badge:
        shape_label(dot, badge, size=13, bold=True, color=WHITE)
    textbox(
        slide,
        left + 0.88,
        top + dot_top + 0.05,
        width - 1.18,
        0.4,
        title,
        size=15,
        bold=True,
        color=NAVY,
    )
    textbox(
        slide,
        left + 0.3,
        top + body_top,
        width - 0.6,
        height - body_top - 0.2,
        body,
        size=12,
        color=GREY,
        line_spacing=1.35,
        space_after=6,
    )


def code_block(slide, left, top, width, height, lines, highlight_from=None):
    shape(slide, left, top, width, height, fill=CODE_BG, radius=0.04)
    box = slide.shapes.add_textbox(
        Inches(left + 0.3), Inches(top + 0.26), Inches(width - 0.6), Inches(height - 0.5)
    )
    frame = box.text_frame
    frame.word_wrap = True
    frame.margin_left = 0
    frame.margin_right = 0
    frame.margin_top = 0
    frame.margin_bottom = 0

    index = 0
    for line in lines:
        if index == 0:
            paragraph = frame.paragraphs[0]
        else:
            paragraph = frame.add_paragraph()
        paragraph.line_spacing = 1.32
        paragraph.space_after = Pt(0)
        run = paragraph.add_run()
        run.text = line
        run.font.size = Pt(13)
        run.font.name = MONO_FONT
        if highlight_from is not None and index >= highlight_from:
            run.font.color.rgb = TEAL
        elif line.strip().startswith("@"):
            run.font.color.rgb = AMBER
        else:
            run.font.color.rgb = RGBColor(0xD6, 0xE2, 0xED)
        index = index + 1


def simple_table(slide, left, top, width, rows, col_widths, header_fill=NAVY):
    """A readable table: navy header row, white body, light rules."""
    row_count = len(rows)
    col_count = len(rows[0])
    height = 0.46 + (0.56 * (row_count - 1))
    graphic = slide.shapes.add_table(
        row_count, col_count, Inches(left), Inches(top), Inches(width), Inches(height)
    )
    table = graphic.table
    table.first_row = True
    table.horz_banding = False

    index = 0
    for fraction in col_widths:
        table.columns[index].width = Inches(width * fraction)
        index = index + 1

    table.rows[0].height = Inches(0.46)
    row_index = 1
    while row_index < row_count:
        table.rows[row_index].height = Inches(0.56)
        row_index = row_index + 1

    row_index = 0
    for row in rows:
        col_index = 0
        for value in row:
            cell = table.cell(row_index, col_index)
            cell.margin_left = Inches(0.16)
            cell.margin_right = Inches(0.12)
            cell.margin_top = Inches(0.07)
            cell.margin_bottom = Inches(0.07)
            cell.vertical_anchor = MSO_ANCHOR.MIDDLE
            cell.fill.solid()
            if row_index == 0:
                cell.fill.fore_color.rgb = header_fill
            elif row_index % 2 == 0:
                cell.fill.fore_color.rgb = WASH
            else:
                cell.fill.fore_color.rgb = WHITE

            frame = cell.text_frame
            frame.word_wrap = True
            paragraph = frame.paragraphs[0]
            paragraph.line_spacing = 1.18
            run = paragraph.add_run()
            run.text = value
            run.font.name = BODY_FONT
            if row_index == 0:
                run.font.size = Pt(12)
                run.font.bold = True
                run.font.color.rgb = WHITE
            else:
                run.font.size = Pt(12)
                run.font.bold = col_index == 0
                if col_index == 0:
                    run.font.color.rgb = NAVY
                else:
                    run.font.color.rgb = GREY
            col_index = col_index + 1
        row_index = row_index + 1
    return graphic


def bullets(slide, left, top, width, height, items, size=14, gap=0.0, accent=TEAL):
    """Bullet list drawn as dot + text so spacing is predictable."""
    row_top = top
    for item in items:
        shape(slide, left, row_top + 0.09, 0.11, 0.11, fill=accent, kind=MSO_SHAPE.OVAL)
        box = textbox(
            slide,
            left + 0.32,
            row_top,
            width - 0.32,
            0.4,
            item,
            size=size,
            color=GREY,
            line_spacing=1.3,
        )
        row_top = row_top + 0.46 + gap
    return row_top


def arrow(slide, left, top, width, height=0.34, fill=None):
    if fill is None:
        fill = RGBColor(0xC2, 0xCE, 0xD9)
    shape(slide, left, top, width, height, fill=fill, kind=MSO_SHAPE.RIGHT_ARROW)


# --------------------------------------------------------------------------
# Slides
# --------------------------------------------------------------------------


def slide_title(prs):
    slide = new_slide(prs, background=NAVY)
    shape(slide, 0, 0, 0.26, SLIDE_H, fill=TEAL, kind=MSO_SHAPE.RECTANGLE)
    shape(slide, 8.9, -1.6, 6.4, 6.4, fill=NAVY_SOFT, kind=MSO_SHAPE.OVAL)

    textbox(
        slide, 1.1, 1.85, 9.0, 0.3, "QUALITY ENGINEERING", size=12, bold=True, color=TEAL
    )
    textbox(
        slide,
        1.1,
        2.3,
        9.4,
        1.9,
        "Test Automation\nFramework",
        size=54,
        bold=True,
        color=WHITE,
        line_spacing=1.02,
    )
    shape(slide, 1.1, 4.42, 1.5, 0.05, fill=AMBER, kind=MSO_SHAPE.RECTANGLE)
    textbox(
        slide,
        1.1,
        4.75,
        9.0,
        0.9,
        "Plain-English tests, running against UI, API and end-to-end.\nBuilt for KEYinfinity. Working today.",
        size=18,
        color=RGBColor(0xB8, 0xC7, 0xD6),
        line_spacing=1.35,
    )
    textbox(
        slide,
        1.1,
        6.45,
        9.0,
        0.4,
        "Prepared for the CTO  ·  QA Automation  ·  October 2026",
        size=12,
        color=GREY_SOFT,
    )


def slide_why(prs):
    slide = new_slide(prs)
    header(slide, "the problem", "Why we need this now", 2)

    card(
        slide,
        MARGIN,
        1.75,
        3.73,
        2.5,
        "Manual testing",
        "Every release needs the same regression checks run again by hand. The list only ever grows, and it competes with testing the new work.",
        accent=CORAL,
        badge="1",
    )
    card(
        slide,
        MARGIN + 4.03,
        1.75,
        3.73,
        2.5,
        "Risk arrives late",
        "Problems are found at the end of a sprint, when the cost of fixing them is highest and there is least time to do it.",
        accent=AMBER,
        badge="2",
    )
    card(
        slide,
        MARGIN + 8.06,
        1.75,
        3.73,
        2.5,
        "An unforgiving domain",
        "This is accounting and tax software. A wrong VAT figure or a leaked tenant record is a customer incident, not a cosmetic bug.",
        accent=VIOLET,
        badge="3",
    )

    shape(slide, MARGIN, 4.7, CONTENT_W, 1.55, fill=WASH, line=BORDER)
    textbox(
        slide, MARGIN + 0.45, 4.98, CONTENT_W - 0.9, 0.35, "WHAT CHANGES", size=10.5, bold=True, color=TEAL
    )
    textbox(
        slide,
        MARGIN + 0.45,
        5.35,
        CONTENT_W - 0.9,
        0.7,
        "The same checks run automatically on every change, in minutes instead of days, and tell us where and why they failed.",
        size=19,
        bold=True,
        color=NAVY,
        line_spacing=1.25,
    )


def slide_idea(prs):
    slide = new_slide(prs)
    header(slide, "the concept", "The whole idea on one slide", 3)

    textbox(
        slide,
        MARGIN,
        1.68,
        CONTENT_W,
        0.5,
        "A sentence written in business language becomes a test that actually runs.",
        size=17,
        color=GREY,
    )

    boxes = [
        ("Plain English", "The test, written\nas a sentence", TEAL),
        ("The glue", "Small C# method\nper sentence", NAVY_SOFT),
        ("The doing", "Page object (screen)\nAPI client (server)", NAVY_SOFT),
        ("The verdict", "Pass, or fail\nwith evidence", AMBER),
    ]

    left = MARGIN
    box_w = 2.62
    gap = 0.44
    index = 0
    for name, detail, colour in boxes:
        shape(slide, left, 2.5, box_w, 1.65, fill=WHITE, line=BORDER)
        shape(slide, left, 2.5, box_w, 0.52, fill=colour, radius=0.14)
        header_box = slide.shapes.add_textbox(
            Inches(left), Inches(2.62), Inches(box_w), Inches(0.3)
        )
        paragraph = header_box.text_frame.paragraphs[0]
        paragraph.alignment = PP_ALIGN.CENTER
        run = paragraph.add_run()
        run.text = name
        run.font.size = Pt(13.5)
        run.font.bold = True
        run.font.color.rgb = WHITE
        run.font.name = BODY_FONT
        textbox(
            slide,
            left + 0.2,
            3.3,
            box_w - 0.4,
            0.8,
            detail,
            size=12.5,
            color=GREY,
            align=PP_ALIGN.CENTER,
            line_spacing=1.3,
            space_after=0,
        )
        if index < 3:
            arrow(slide, left + box_w + 0.08, 3.16, gap - 0.16)
        left = left + box_w + gap
        index = index + 1

    business_band = shape(slide, MARGIN, 4.42, 5.72, 0.42, fill=WASH, radius=0.3)
    shape_label(
        business_band, "Product and QA read and review this row", size=12, bold=True, color=NAVY
    )
    engineering_band = shape(slide, MARGIN + 6.11, 4.42, 5.72, 0.42, fill=WASH, radius=0.3)
    shape_label(
        engineering_band, "Engineering owns and maintains this row", size=12, bold=True, color=NAVY
    )

    shape(slide, MARGIN, 5.3, CONTENT_W, 1.35, fill=NAVY, radius=0.05)
    textbox(
        slide,
        MARGIN + 0.5,
        5.6,
        CONTENT_W - 1.0,
        0.8,
        "One artefact, two audiences. The business agrees what should be true; engineering owns how it is proved.",
        size=17,
        bold=True,
        color=WHITE,
        line_spacing=1.3,
    )


def slide_example(prs):
    slide = new_slide(prs)
    header(slide, "what it looks like", "A real test, exactly as it is written", 4)

    code_block(
        slide,
        MARGIN,
        1.72,
        7.1,
        2.85,
        [
            "@api @smoke @marketing",
            "Feature: Page lookup",
            "",
            "  Scenario: A known page is found by its slug",
            "    Given the content API is available",
            '    When the page "technical" is looked up',
            "    Then the page is returned",
            '    And the page title is "Technical"',
        ],
    )

    textbox(
        slide,
        MARGIN + 7.5,
        1.72,
        4.33,
        0.4,
        "NOTICE WHAT IS NOT THERE",
        size=10.5,
        bold=True,
        color=TEAL,
    )
    bullets(
        slide,
        MARGIN + 7.5,
        2.22,
        4.33,
        2.4,
        [
            "No web address",
            "No screen selectors",
            "No programming syntax",
            "No test data buried in code",
        ],
        size=14,
    )
    shape(slide, MARGIN + 7.5, 4.35, 4.33, 0.87, fill=WASH, line=BORDER)
    textbox(
        slide,
        MARGIN + 7.72,
        4.55,
        3.9,
        0.5,
        "A product manager can review it.\nAn engineer can run it.",
        size=13,
        bold=True,
        color=NAVY,
        line_spacing=1.3,
    )

    shape(slide, MARGIN, 5.55, CONTENT_W, 1.1, fill=WHITE, line=BORDER)
    textbox(
        slide,
        MARGIN + 0.4,
        5.72,
        CONTENT_W - 0.8,
        0.8,
        "The three tags on the first line are the control panel: layer (@api), depth (@smoke) and area (@marketing). "
        "The build pipeline picks what to run with them — a two-minute smoke run on a pull request, the full set overnight.",
        size=13.5,
        color=GREY,
        line_spacing=1.35,
    )


def slide_layers(prs):
    slide = new_slide(prs)
    header(slide, "test strategy", "Three layers, one rule", 5)

    tiers = [
        ("End-to-end", "1 scenario", "Proves two layers agree with each other", 3.0, CORAL, 1.9),
        ("User interface", "8 scenarios", "Proves what a person sees in a real Chrome browser", 5.4, AMBER, 2.82),
        ("API", "9 scenarios", "Proves server rules: status codes, data shape, boundaries", 7.8, TEAL, 3.74),
    ]
    for name, count, detail, width, colour, top in tiers:
        left = MARGIN + ((7.8 - width) / 2)
        block = shape(slide, left, top, width, 0.78, fill=colour, radius=0.1)
        shape_label(block, name + "   ·   " + count, size=15, bold=True, color=WHITE)
        textbox(
            slide,
            MARGIN + 8.1,
            top + 0.14,
            3.73,
            0.6,
            detail,
            size=12.5,
            color=GREY,
            line_spacing=1.25,
        )

    textbox(
        slide,
        MARGIN + 0.1,
        4.68,
        7.6,
        0.3,
        "Wider band = more tests. Lower band = faster, steadier, cheaper to keep.",
        size=11.5,
        color=GREY_SOFT,
        align=PP_ALIGN.CENTER,
    )

    shape(slide, MARGIN, 5.25, CONTENT_W, 1.4, fill=NAVY, radius=0.05)
    textbox(
        slide, MARGIN + 0.5, 5.5, CONTENT_W - 1.0, 0.3, "THE RULE", size=10.5, bold=True, color=TEAL
    )
    textbox(
        slide,
        MARGIN + 0.5,
        5.85,
        CONTENT_W - 1.0,
        0.6,
        "Always prove it at the lowest layer that can. Driving a browser to check a server rule costs a minute a run and proves nothing extra.",
        size=16,
        bold=True,
        color=WHITE,
        line_spacing=1.3,
    )


def slide_structure(prs):
    slide = new_slide(prs)
    header(slide, "how it is built", "Four layers, each with one job", 6)

    rows = [
        (
            "Feature files",
            "The test in plain English. No code, no addresses, no selectors.",
            "tests/*/Features",
            TEAL,
        ),
        (
            "Step definitions",
            "One small C# method per sentence. The only place a test decides pass or fail.",
            "tests/*/Steps",
            NAVY_SOFT,
        ),
        (
            "Page objects  &  API clients",
            "The only code that knows how a screen is built or how a request is made.",
            "QAAutomation.UI / .Api",
            NAVY_SOFT,
        ),
        (
            "Core",
            "Shared plumbing: settings, browser start-up and shut-down, waiting, failure capture.",
            "QAAutomation.Core",
            AMBER,
        ),
    ]

    top = 1.74
    for name, detail, path, colour in rows:
        shape(slide, MARGIN, top, 9.0, 0.94, fill=WHITE, line=BORDER)
        shape(slide, MARGIN, top, 0.13, 0.94, fill=colour, kind=MSO_SHAPE.RECTANGLE)
        textbox(slide, MARGIN + 0.4, top + 0.15, 5.1, 0.3, name, size=14.5, bold=True, color=NAVY)
        textbox(
            slide,
            MARGIN + 0.4,
            top + 0.5,
            8.2,
            0.35,
            detail,
            size=12,
            color=GREY,
            line_spacing=1.25,
        )
        textbox(
            slide,
            MARGIN + 6.3,
            top + 0.17,
            2.6,
            0.3,
            path,
            size=10.5,
            color=GREY_SOFT,
            font=MONO_FONT,
            align=PP_ALIGN.RIGHT,
        )
        top = top + 1.04

    shape(slide, MARGIN + 9.35, 1.74, 2.48, 3.86, fill=WASH, line=BORDER)
    textbox(
        slide, MARGIN + 9.62, 2.0, 2.0, 0.3, "WHY IT PAYS", size=10.5, bold=True, color=TEAL
    )
    bullets(
        slide,
        MARGIN + 9.62,
        2.46,
        1.95,
        2.8,
        [
            "A screen changes:\none file to fix",
            "An address changes:\none setting to edit",
            "Dependencies run\none way only",
        ],
        size=12,
        gap=0.56,
    )

    shape(slide, MARGIN, 5.9, CONTENT_W, 0.9, fill=WHITE, line=BORDER)
    textbox(
        slide,
        MARGIN + 0.4,
        6.08,
        CONTENT_W - 0.8,
        0.6,
        "The point of the split: a test says what should be true, and nothing else. Everything about how the product is built lives one layer below, "
        "so a redesign costs an afternoon rather than a rewrite.",
        size=13.5,
        color=GREY,
        line_spacing=1.35,
    )


def slide_stack(prs):
    slide = new_slide(prs)
    header(slide, "the tools", "What it is made of, and why", 7)

    simple_table(
        slide,
        MARGIN,
        1.7,
        CONTENT_W,
        [
            ["What we needed", "What we chose", "Why that one"],
            [
                "Plain-English test runner",
                "Reqnroll",
                "SpecFlow, the old standard, was abandoned in 2024. Reqnroll is the maintained successor.",
            ],
            [
                "Browser control",
                "Selenium WebDriver",
                "Industry standard, and the team can already debug it. See the next slide.",
            ],
            [
                "Server requests",
                "Refit",
                "API contracts are declared once, in code the compiler checks. Nobody builds a web address by joining strings.",
            ],
            [
                "Readable assertions",
                "AwesomeAssertions",
                "Apache-2.0 licensed. The popular alternative became paid-per-seat — see the next slide.",
            ],
            [
                "Settings and secrets",
                "Microsoft.Extensions.Configuration",
                "Committed files hold placeholders only. Real values arrive as environment variables.",
            ],
        ],
        [0.21, 0.28, 0.51],
    )

    shape(slide, MARGIN, 5.75, CONTENT_W, 0.95, fill=WASH, line=BORDER)
    textbox(
        slide,
        MARGIN + 0.4,
        5.95,
        CONTENT_W - 0.8,
        0.6,
        "Every version is pinned in one file at the repository root. No floating version ranges, so a build that works today works the same next month.",
        size=13.5,
        bold=True,
        color=NAVY,
        line_spacing=1.3,
    )


def slide_decisions(prs):
    slide = new_slide(prs)
    header(slide, "trade-offs", "Two decisions worth your attention", 8)

    shape(slide, MARGIN, 1.72, 5.72, 4.3, fill=WHITE, line=BORDER)
    left_heading = shape(slide, MARGIN, 1.72, 5.72, 0.62, fill=NAVY, radius=0.12)
    shape_label(left_heading, "Selenium, not Playwright", size=16, bold=True, color=WHITE)
    textbox(
        slide,
        MARGIN + 0.4,
        2.62,
        4.92,
        0.9,
        "Playwright is the stronger tool on paper. We chose Selenium anyway, for one reason that outranks the feature list: the team can own and debug it on a release morning.",
        size=13,
        color=GREY,
        line_spacing=1.35,
    )
    textbox(slide, MARGIN + 0.4, 3.75, 4.92, 0.3, "THE COST, AND WHAT WE DID", size=10.5, bold=True, color=AMBER)
    bullets(
        slide,
        MARGIN + 0.4,
        4.15,
        4.92,
        1.6,
        [
            "Selenium never waits by itself, so every wait goes through one shared helper. Fixed pauses are banned.",
            "There is no built-in failure viewer, so we capture our own evidence. Next slide.",
        ],
        size=12.5,
        gap=0.52,
        accent=AMBER,
    )

    shape(slide, MARGIN + 6.11, 1.72, 5.72, 4.3, fill=WHITE, line=BORDER)
    right_heading = shape(slide, MARGIN + 6.11, 1.72, 5.72, 0.62, fill=NAVY, radius=0.12)
    shape_label(right_heading, "A licence we avoided", size=16, bold=True, color=WHITE)
    textbox(
        slide,
        MARGIN + 6.51,
        2.62,
        4.92,
        0.9,
        "FluentAssertions is the best-known assertion library in .NET. Version 8 moved to a commercial, paid-per-seat licence. Our product is commercial, so taking it creates a liability.",
        size=13,
        color=GREY,
        line_spacing=1.35,
    )
    textbox(slide, MARGIN + 6.51, 3.75, 4.92, 0.3, "WHAT WE USE INSTEAD", size=10.5, bold=True, color=TEAL)
    bullets(
        slide,
        MARGIN + 6.51,
        4.15,
        4.92,
        1.6,
        [
            "AwesomeAssertions: the Apache-2.0 community fork, near-identical to use.",
            "No per-seat cost, no licence review, and no code to rewrite later.",
        ],
        size=12.5,
        gap=0.52,
    )

    textbox(
        slide,
        MARGIN,
        6.25,
        CONTENT_W,
        0.4,
        "Both decisions are written down with their reasoning, so the next person inherits the argument, not just the outcome.",
        size=13,
        color=GREY_SOFT,
        align=PP_ALIGN.CENTER,
    )


def slide_evidence(prs):
    slide = new_slide(prs)
    header(slide, "diagnosis", "When a test fails, we know why", 9)

    textbox(
        slide,
        MARGIN,
        1.68,
        CONTENT_W,
        0.45,
        "An intermittent failure nobody can explain is worse than no test at all. So every failed browser test writes four files.",
        size=16,
        color=GREY,
    )

    items = [
        ("screenshot.png", "What the user actually saw", TEAL),
        ("page-source.html", "Was the element missing, or just hidden", NAVY_SOFT),
        ("console.log", "Did the page's own code throw an error", AMBER),
        ("location.txt", "The real address, title and run id", VIOLET),
    ]
    left = MARGIN
    width = 2.84
    for name, detail, colour in items:
        shape(slide, left, 2.5, width, 1.75, fill=WHITE, line=BORDER)
        shape(slide, left + 0.3, 2.78, 0.5, 0.1, fill=colour, kind=MSO_SHAPE.RECTANGLE)
        textbox(
            slide, left + 0.3, 3.02, width - 0.6, 0.3, name, size=13, bold=True, color=NAVY, font=MONO_FONT
        )
        textbox(
            slide,
            left + 0.3,
            3.42,
            width - 0.6,
            0.7,
            detail,
            size=12.5,
            color=GREY,
            line_spacing=1.3,
        )
        left = left + width + 0.16

    path_band = shape(slide, MARGIN, 4.55, CONTENT_W, 0.6, fill=WASH, radius=0.1)
    shape_label(
        path_band,
        "test-results \\ <suite> \\ <scenario name>-<timestamp> \\",
        size=13,
        bold=True,
        color=NAVY,
        font=MONO_FONT,
    )

    card(
        slide,
        MARGIN,
        5.35,
        5.72,
        1.45,
        "Nothing is kept when tests pass",
        "A green run writes no files, so an empty folder is the healthy state.",
        accent=TEAL,
        dot_top=0.2,
        body_top=0.8,
    )
    card(
        slide,
        MARGIN + 6.11,
        5.35,
        5.72,
        1.45,
        "Treat the folder as sensitive",
        "It is whatever the app really sent. Masking comes before we point this at a product with a login.",
        accent=CORAL,
        dot_top=0.2,
        body_top=0.8,
    )


def slide_guardrails(prs):
    slide = new_slide(prs)
    header(slide, "quality of the tests themselves", "Guardrails, not good intentions", 10)

    textbox(
        slide,
        MARGIN,
        1.68,
        CONTENT_W,
        0.42,
        "A suite nobody trusts gets ignored, then deleted. Four rules keep it trustworthy, three enforced by machine.",
        size=15.5,
        color=GREY,
    )

    rows = [
        (
            "No secrets in the repository",
            "A git hook scans every commit for credential-shaped values, masks what it finds, and blocks the commit. If the scanner itself fails, the commit is still blocked.",
            "Automated",
            CORAL,
        ),
        (
            "No fixed pauses",
            "Every wait goes through one shared helper that waits for a condition and gives up on a timeout. A slow page costs a second, not ten.",
            "Automated",
            AMBER,
        ),
        (
            "Code a manual tester can read",
            "The clever, compact C# idioms are banned on purpose. The build raises a warning naming the file that broke the rule.",
            "Automated",
            TEAL,
        ),
        (
            "Judgement lives in one place only",
            "A page object reports what it sees; a client reports what the server said. Only the test decides pass or fail.",
            "Code review",
            VIOLET,
        ),
    ]

    top = 2.42
    for name, detail, enforcement, colour in rows:
        shape(slide, MARGIN, top, CONTENT_W, 1.0, fill=WHITE, line=BORDER)
        shape(slide, MARGIN, top, 0.13, 1.0, fill=colour, kind=MSO_SHAPE.RECTANGLE)
        textbox(slide, MARGIN + 0.4, top + 0.15, 7.4, 0.3, name, size=14.5, bold=True, color=NAVY)
        textbox(
            slide,
            MARGIN + 0.4,
            top + 0.5,
            9.6,
            0.42,
            detail,
            size=12,
            color=GREY,
            line_spacing=1.25,
        )
        tag = shape(slide, MARGIN + 10.25, top + 0.29, 1.4, 0.42, fill=WASH, radius=0.3)
        shape_label(tag, enforcement, size=11, bold=True, color=NAVY)
        top = top + 1.08


def slide_today(prs):
    slide = new_slide(prs)
    header(slide, "status", "Where it stands today", 11)

    stats = [
        ("18", "scenarios passing", TEAL),
        ("6", "capability areas", NAVY_SOFT),
        ("0", "build warnings", AMBER),
        ("~2 min", "full run", VIOLET),
    ]
    left = MARGIN
    width = 2.84
    for value, label, colour in stats:
        shape(slide, left, 1.78, width, 1.62, fill=WHITE, line=BORDER)
        textbox(
            slide,
            left + 0.2,
            2.02,
            width - 0.4,
            0.75,
            value,
            size=42,
            bold=True,
            color=colour,
            align=PP_ALIGN.CENTER,
        )
        textbox(
            slide,
            left + 0.2,
            2.88,
            width - 0.4,
            0.35,
            label,
            size=13,
            color=GREY,
            align=PP_ALIGN.CENTER,
        )
        left = left + width + 0.16

    textbox(
        slide, MARGIN, 3.75, 5.72, 0.3, "WHAT IS COVERED", size=10.5, bold=True, color=TEAL
    )
    bullets(
        slide,
        MARGIN,
        4.15,
        5.72,
        2.2,
        [
            "API: page listing, page lookup, pagination rules, error handling",
            "Browser: home page, primary navigation, enquiry form validation",
            "End-to-end: the API and the rendered page agreeing on one record",
        ],
        size=13,
        gap=0.26,
    )

    shape(slide, MARGIN + 6.11, 3.7, 5.72, 2.95, fill=WASH, line=BORDER)
    textbox(
        slide, MARGIN + 6.51, 3.98, 5.0, 0.3, "AND BE CLEAR ABOUT THIS", size=10.5, bold=True, color=AMBER
    )
    textbox(
        slide,
        MARGIN + 6.51,
        4.38,
        4.92,
        2.0,
        "It runs against a public marketing website, standing in for KEYinfinity until that is available.\n\n"
        "Every request is read-only. Nothing is created, changed or submitted.\n\n"
        "It proves the framework works end to end. It does not yet prove anything about our product.",
        size=12.5,
        color=GREY,
        line_spacing=1.35,
        space_after=2,
    )


def slide_switch(prs):
    slide = new_slide(prs)
    header(slide, "the handover", "Pointing it at KEYinfinity takes three steps", 12)

    steps = [
        (
            "1",
            "Change two settings",
            "The product address and the API address live in one settings file. Nothing else moves.",
            TEAL,
        ),
        (
            "2",
            "Agree test identifiers",
            "Our front end can mark elements with a dedicated test attribute. That makes browser tests near-unbreakable by redesign. It needs to be a standing agreement with the front-end team.",
            AMBER,
        ),
        (
            "3",
            "Write the real tests",
            "Swap the example features for KEYinfinity capabilities. The structure, the rules and the plumbing all stay exactly as they are.",
            VIOLET,
        ),
    ]

    left = MARGIN
    width = 3.73
    for number, name, detail, colour in steps:
        shape(slide, left, 1.8, width, 3.45, fill=WHITE, line=BORDER)
        badge = shape(slide, left + 0.35, 2.12, 0.62, 0.62, fill=colour, kind=MSO_SHAPE.OVAL)
        shape_label(badge, number, size=22, bold=True, color=WHITE)
        textbox(slide, left + 0.35, 3.0, width - 0.7, 0.6, name, size=17, bold=True, color=NAVY, line_spacing=1.2)
        textbox(
            slide,
            left + 0.35,
            3.78,
            width - 0.7,
            1.3,
            detail,
            size=12.5,
            color=GREY,
            line_spacing=1.35,
        )
        left = left + width + 0.3

    shape(slide, MARGIN, 5.6, CONTENT_W, 1.05, fill=NAVY, radius=0.05)
    textbox(
        slide,
        MARGIN + 0.5,
        5.85,
        CONTENT_W - 1.0,
        0.6,
        "Step 2 is the one that needs your backing. A test attribute owned by the front end is the difference between tests that survive a redesign and tests that break on every one.",
        size=15,
        bold=True,
        color=WHITE,
        line_spacing=1.3,
    )


def slide_roadmap(prs):
    slide = new_slide(prs)
    header(slide, "plan", "How it rolls out", 13)

    phases = [
        (
            "Phase 1",
            "Done",
            "Framework, house rules, failure evidence, secret scanning, 18 passing tests proving the shape works.",
            TEAL,
        ),
        (
            "Phase 2",
            "Next",
            "Build pipeline in Azure DevOps. Smoke tests on every pull request, the full set overnight, results published to the build.",
            AMBER,
        ),
        (
            "Phase 3",
            "On KEYinfinity access",
            "Real coverage: VAT returns and MTD submission, bank reconciliation, invoicing and payments, roles and permissions.",
            VIOLET,
        ),
        (
            "Phase 4",
            "Hardening",
            "Tenant isolation tests, a stub standing in for HMRC so no pipeline ever calls a real tax authority, throwaway databases per run.",
            NAVY_SOFT,
        ),
    ]

    shape(slide, MARGIN + 0.1, 2.72, CONTENT_W - 0.2, 0.06, fill=BORDER, kind=MSO_SHAPE.RECTANGLE)

    left = MARGIN
    width = 2.84
    for name, state, detail, colour in phases:
        marker = shape(slide, left + (width / 2) - 0.17, 2.58, 0.34, 0.34, fill=colour, kind=MSO_SHAPE.OVAL)
        textbox(
            slide,
            left,
            1.9,
            width,
            0.3,
            name,
            size=15,
            bold=True,
            color=NAVY,
            align=PP_ALIGN.CENTER,
        )
        chip = shape(slide, left + (width / 2) - 0.85, 2.22, 1.7, 0.3, fill=colour, radius=0.4)
        shape_label(chip, state, size=10.5, bold=True, color=WHITE)
        shape(slide, left, 3.15, width, 1.95, fill=WHITE, line=BORDER)
        textbox(
            slide,
            left + 0.28,
            3.4,
            width - 0.56,
            1.5,
            detail,
            size=12.5,
            color=GREY,
            line_spacing=1.4,
            space_after=0,
        )
        left = left + width + 0.16

    textbox(
        slide,
        MARGIN,
        5.5,
        CONTENT_W,
        0.6,
        "Phases 2 and 3 can run in parallel. The pipeline does not need the product, and the product does not need the pipeline.",
        size=14,
        color=GREY_SOFT,
        align=PP_ALIGN.CENTER,
    )


def slide_needs(prs):
    slide = new_slide(prs)
    header(slide, "dependencies", "What the framework needs from engineering", 14)

    rows = [
        [
            "Test identifiers in the front end",
            "Our model-driven platform emits a test attribute on generated elements, treated as a contract rather than a favour.",
            "High",
        ],
        [
            "An environment automation can run against",
            "Stable, resettable, and not production. Browser tests need a real deployment.",
            "High",
        ],
        [
            "Every integration pointable at a stub",
            "A pipeline must never call HMRC. We supply the stub; we need the switch to exist in every environment.",
            "High",
        ],
        [
            "Two seeded test tenants",
            "Tenant isolation is a functional requirement. It cannot be tested with a single tenant.",
            "Medium",
        ],
        [
            "Thirty minutes with the product manager",
            "Fifteen open domain questions: the role model, the VAT return lifecycle, what counts as a cross-tenant refusal.",
            "Medium",
        ],
    ]

    table_rows = [["What we need", "Why", "Priority"]]
    for row in rows:
        table_rows.append(row)

    simple_table(slide, MARGIN, 1.75, CONTENT_W, table_rows, [0.28, 0.58, 0.14])

    shape(slide, MARGIN, 5.8, CONTENT_W, 0.85, fill=WASH, line=BORDER)
    textbox(
        slide,
        MARGIN + 0.4,
        5.98,
        CONTENT_W - 0.8,
        0.5,
        "None of these are large pieces of work. All of them are cheaper to agree now than to retrofit after the first hundred tests are written.",
        size=13.5,
        bold=True,
        color=NAVY,
    )


def slide_limits(prs):
    slide = new_slide(prs)
    header(slide, "honesty", "What this does not do yet", 15)

    textbox(
        slide,
        MARGIN,
        1.68,
        CONTENT_W,
        0.42,
        "Stated plainly, so nobody discovers it later and reasonably asks why they were not told.",
        size=16,
        color=GREY,
    )

    items = [
        (
            "No login, tenants or database in the stand-in site",
            "So nothing here demonstrates authentication, tenant isolation or data setup. Those are real KEYinfinity concerns and they are not faked.",
        ),
        (
            "No build pipeline yet",
            "Parked deliberately while the suite settled. The conventions are already written down, so wiring it up is configuration rather than design.",
        ),
        (
            "Chrome only",
            "Any other browser fails with a clear configuration error. Adding Edge or Firefox is a later, deliberate decision.",
        ),
        (
            "Live third-party content is a standing fragility",
            "Expected page titles and link names are marketing copy, not contract. They are concentrated in one settings file so a copy change is a one-line edit.",
        ),
    ]

    top = 2.35
    for name, detail in items:
        shape(slide, MARGIN, top, CONTENT_W, 1.02, fill=WHITE, line=BORDER)
        shape(slide, MARGIN + 0.35, top + 0.36, 0.3, 0.3, fill=AMBER, kind=MSO_SHAPE.OVAL)
        textbox(slide, MARGIN + 0.92, top + 0.16, 10.6, 0.3, name, size=14.5, bold=True, color=NAVY)
        textbox(
            slide,
            MARGIN + 0.92,
            top + 0.52,
            10.6,
            0.45,
            detail,
            size=12,
            color=GREY,
            line_spacing=1.25,
        )
        top = top + 1.12


def slide_ask(prs):
    slide = new_slide(prs, background=NAVY)
    shape(slide, 0, 0, 0.26, SLIDE_H, fill=TEAL, kind=MSO_SHAPE.RECTANGLE)

    textbox(slide, 1.1, 1.0, 10.0, 0.3, "THE ASK", size=12, bold=True, color=TEAL)
    textbox(
        slide,
        1.1,
        1.4,
        10.5,
        0.8,
        "Four decisions and we can start",
        size=38,
        bold=True,
        color=WHITE,
    )

    asks = [
        ("Endorse the test attribute", "as a front-end contract, owned by the platform team"),
        ("Approve the pipeline stage", "smoke on every pull request, full regression overnight"),
        ("Confirm environment and stub policy", "including the rule that no pipeline calls a real tax authority"),
        ("Book thirty minutes with the product manager", "to close the fifteen open domain questions"),
    ]

    top = 2.7
    number = 1
    for name, detail in asks:
        badge = shape(slide, 1.1, top, 0.46, 0.46, fill=TEAL, kind=MSO_SHAPE.OVAL)
        shape_label(badge, str(number), size=14, bold=True, color=NAVY)
        textbox(slide, 1.78, top + 0.02, 9.6, 0.3, name, size=18, bold=True, color=WHITE)
        textbox(
            slide, 1.78, top + 0.38, 9.6, 0.3, detail, size=13, color=RGBColor(0x9F, 0xB2, 0xC4)
        )
        top = top + 0.98
        number = number + 1

    shape(slide, 1.1, 6.6, 11.0, 0.05, fill=AMBER, kind=MSO_SHAPE.RECTANGLE)
    textbox(
        slide,
        1.1,
        6.82,
        11.0,
        0.3,
        "The framework is built and proven. What it needs now is a product to point at.",
        size=13,
        color=GREY_SOFT,
    )


# --------------------------------------------------------------------------
# Build
# --------------------------------------------------------------------------


def build():
    prs = Presentation()
    prs.slide_width = Inches(SLIDE_W)
    prs.slide_height = Inches(SLIDE_H)

    slide_title(prs)
    slide_why(prs)
    slide_idea(prs)
    slide_example(prs)
    slide_layers(prs)
    slide_structure(prs)
    slide_stack(prs)
    slide_decisions(prs)
    slide_evidence(prs)
    slide_guardrails(prs)
    slide_today(prs)
    slide_switch(prs)
    slide_roadmap(prs)
    slide_needs(prs)
    slide_limits(prs)
    slide_ask(prs)

    here = os.path.dirname(os.path.abspath(__file__))
    target = os.path.join(here, "QAAutomation-Framework-Overview.pptx")
    prs.save(target)
    print("Wrote " + target)
    print("Slides: " + str(len(prs.slides)))


if __name__ == "__main__":
    build()

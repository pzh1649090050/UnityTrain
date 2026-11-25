"""将教材 Markdown 导出为可编辑 Word 与带目录的离线 HTML。使用工作区捆绑 Python 运行。"""
import html
import re
from pathlib import Path

from docx import Document
from docx.oxml import OxmlElement
from docx.oxml.ns import qn
from docx.shared import Cm, Pt, RGBColor

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / 'Docs/Unity风扇与巡检培训手册.md'


def blocks(text):
    lines = text.splitlines()
    i = 0
    while i < len(lines):
        line = lines[i]
        if not line.strip():
            i += 1
            continue
        if line.startswith('```'):
            body = []
            i += 1
            while i < len(lines) and not lines[i].startswith('```'):
                body.append(lines[i]); i += 1
            yield 'code', '\n'.join(body)
        elif line.startswith('|'):
            rows = []
            while i < len(lines) and lines[i].startswith('|'):
                cells = [x.strip() for x in lines[i].strip('|').split('|')]
                if not all(re.fullmatch(r'[: -]+', x) for x in cells): rows.append(cells)
                i += 1
            yield 'table', rows
            continue
        elif line.startswith('#'):
            match = re.match(r'^(#+) (.*)', line)
            yield 'heading', (len(match[1]), match[2])
        elif re.match(r'^\d+\. ', line):
            yield 'number', line
        elif line.startswith('- '):
            yield 'bullet', line[2:]
        else:
            yield 'paragraph', line
        i += 1


def plain(text):
    return re.sub(r'\[([^]]+)\]\(([^)]+)\)', r'\1（\2）', text).replace('**', '').replace('`', '')


def html_inline(text):
    escaped = html.escape(text)
    escaped = re.sub(r'\[([^]]+)\]\(([^)]+)\)', r'<a href="\2">\1</a>', escaped)
    escaped = re.sub(r'`([^`]+)`', r'<code>\1</code>', escaped)
    return re.sub(r'\*\*([^*]+)\*\*', r'<strong>\1</strong>', escaped)


def set_font(style, size):
    style.font.name = 'Microsoft YaHei'
    style.font.size = Pt(size)
    style.font.color.rgb = RGBColor(0, 0, 0)
    style.element.get_or_add_rPr().rFonts.set(qn('w:eastAsia'), 'Microsoft YaHei')


def export():
    content = SOURCE.read_text(encoding='utf-8')
    source_script = (ROOT / 'Assets/Training/Scripts/FanTrainingController.cs').read_text(encoding='utf-8')
    content += '\n\n## 附录 主讲脚本完整源码\n\n以下为交付工程中的实际脚本，可对照 Inspector 字段与按钮事件阅读。\n\n```csharp\n' + source_script + '\n```\n'
    doc = Document()
    section = doc.sections[0]
    section.page_width = Cm(21); section.page_height = Cm(29.7)
    section.top_margin = section.bottom_margin = Cm(2)
    section.left_margin = section.right_margin = Cm(2.1)
    for name, size in [('Normal', 10.5), ('Title', 27), ('Heading 1', 17), ('Heading 2', 13), ('Heading 3', 11)]:
        set_font(doc.styles[name], size)
        doc.styles[name].font.underline = False
    for border in list(doc.styles.element.iter(qn('w:pBdr'))): border.getparent().remove(border)
    normal = doc.styles['Normal'].paragraph_format
    normal.line_spacing = 1.25; normal.space_after = Pt(6)
    for name in ['Heading 1','Heading 2','Heading 3']:
        doc.styles[name].paragraph_format.keep_with_next = True
        doc.styles[name].paragraph_format.space_before = Pt(13)
    doc.core_properties.title = 'Unity 风扇动画与巡检培训手册'
    doc.core_properties.subject = 'Unity 2022.3.55f1c1 全流程培训'
    header = section.header.paragraphs[0]
    header.text = 'UnityTrain 培训教材    Unity 2022.3.55f1c1'
    header.style = doc.styles['Normal']
    footer = section.footer.paragraphs[0]; footer.alignment = 2
    footer.add_run('第 ')
    field = OxmlElement('w:fldSimple'); field.set(qn('w:instr'), 'PAGE'); footer._p.append(field)
    footer.add_run(' 页')
    html_parts, toc = [], []
    heading_index = 0
    for kind, value in blocks(content):
        if kind == 'heading':
            level, title = value
            if level == 1:
                doc.add_paragraph(title, 'Title')
                doc.add_paragraph('学员操作手册与讲师授课参考\n版本 2026 年 11 月 19 日\nCinemachine 2.9.7   Timeline 1.6.2')
                if (ROOT / 'Docs/Images/fan-training.png').exists():
                    doc.add_picture(str(ROOT / 'Docs/Images/fan-training.png'), width=Cm(16.5))
                doc.add_paragraph('使用顺序：准备环境 → 搭建模型 → 制作动画 → 连接按钮 → 编排巡检 → 扩展 HTTP 与 Editor。')
                doc.add_page_break()
                doc.add_heading('课程目录', level=1)
                # 静态目录保证无需更新 Word 域即可读；正文标题仍支持导航窗格。
                for title_item in re.findall(r'^## (.+)$', content, re.MULTILINE):
                    doc.add_paragraph(title_item)
                doc.add_page_break()
            else:
                heading = doc.add_heading(title, level=min(level - 1, 3))
                if title.startswith('附录'): heading.paragraph_format.page_break_before = True
            heading_index += 1
            anchor = 'chapter-' + str(heading_index)
            html_parts.append(f'<h{level} id="{anchor}">{html_inline(title)}</h{level}>')
            if level == 1 and (ROOT / 'Docs/Images/fan-training.png').exists():
                html_parts.append('<img src="Images/fan-training.png" alt="风扇主讲场景实际画面">')
            if level == 2: toc.append(f'<a href="#{anchor}">{html.escape(title)}</a>')
        elif kind == 'table':
            table = doc.add_table(rows=0, cols=len(value[0])); table.style = 'Light Shading Accent 1'; table.autofit = False
            widths = [16.8 / len(value[0])] * len(value[0])
            if len(widths) == 2: widths = [5, 11.8]
            if len(widths) == 3: widths = [3.5, 8.3, 5]
            if len(widths) == 4: widths = [3.2, 4.1, 5, 4.5]
            for column, width in zip(table.columns, widths): column.width = Cm(width)
            for row_index, cells in enumerate(value):
                row = table.add_row()
                for index, text in enumerate(cells):
                    cell = row.cells[index]; cell.width = Cm(widths[index]); cell.text = plain(text)
                    for para in cell.paragraphs:
                        para.paragraph_format.space_after = Pt(4)
                        for run in para.runs: run.font.size = Pt(9)
                    margin = OxmlElement('w:tcMar')
                    for edge in ['top','left','bottom','right']:
                        node = OxmlElement('w:' + edge); node.set(qn('w:w'), '90'); node.set(qn('w:type'), 'dxa'); margin.append(node)
                    cell._tc.get_or_add_tcPr().append(margin)
                cant_split = OxmlElement('w:cantSplit'); row._tr.get_or_add_trPr().append(cant_split)
                if row_index == 0:
                    repeat = OxmlElement('w:tblHeader'); row._tr.get_or_add_trPr().append(repeat)
            doc.add_paragraph()
            html_parts.append('<div class="table-wrap"><table>' + ''.join('<tr>' + ''.join(f'<{"th" if i == 0 else "td"}>{html_inline(cell)}</{"th" if i == 0 else "td"}>' for cell in row) + '</tr>' for i, row in enumerate(value)) + '</table></div>')
        elif kind == 'code':
            for line in value.splitlines():
                para = doc.add_paragraph(); para.paragraph_format.space_after = Pt(0)
                para.paragraph_format.keep_with_next = bool(line.strip()) and not line.startswith('        }')
                run = para.add_run(line); run.font.name = 'Consolas'; run.font.size = Pt(8)
                run._element.get_or_add_rPr().rFonts.set(qn('w:eastAsia'), 'Microsoft YaHei')
            html_parts.append('<pre><code>' + html.escape(value) + '</code></pre>')
        else:
            para = doc.add_paragraph(plain(value), 'List Bullet' if kind == 'bullet' else 'Normal')
            html_parts.append('<p>' + html_inline(value) + '</p>')
    destination = SOURCE.with_suffix('.docx'); doc.save(destination)
    page = '''<!doctype html><html lang="zh-CN"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Unity 风扇动画与巡检培训手册</title><style>
body{margin:0;color:#172b3a;background:#f5f7fa;font:16px/1.8 'Microsoft YaHei',sans-serif}nav{position:fixed;inset:0 auto 0 0;width:250px;overflow:auto;padding:28px 22px;background:#0d2532;color:white}nav a{display:block;color:#c7e5e5;text-decoration:none;font-size:14px;margin:8px 0}main{max-width:1000px;margin-left:300px;padding:38px 45px;background:white}h1{font-size:34px}h2{margin-top:48px;border-bottom:1px solid #d4e0e5;padding-bottom:10px}h3{margin-top:26px}table{border-collapse:collapse;width:100%;font-size:14px;margin:20px 0}th,td{border:1px solid #d4e0e5;padding:10px;text-align:left;vertical-align:top}th{background:#e8f2f3}pre{white-space:pre-wrap;overflow-wrap:anywhere;background:#f1f4f7;padding:16px;font-size:13px}code{font-family:Consolas,monospace}a{color:#08756b}.table-wrap{overflow:auto}img{max-width:100%}@media(max-width:950px){nav{position:static;width:auto}main{margin:0;padding:24px}}@media print{nav{display:none}main{margin:0;padding:0;max-width:none}body{background:white}h2,h3{break-after:avoid}tr{break-inside:avoid}thead{display:table-header-group}}
</style><nav><strong>课程目录</strong>''' + ''.join(toc) + '</nav><main>' + ''.join(html_parts) + '</main></html>'
    SOURCE.with_suffix('.html').write_text(page, encoding='utf-8')
    print('Exported', destination, 'and HTML')


if __name__ == '__main__':
    export()

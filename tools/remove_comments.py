import io
import os

def strip_comments_from_text(s):
    out = []
    i = 0
    n = len(s)
    in_s = False
    in_c = False
    in_v = False
    in_ml = False
    while i < n:
        c = s[i]
        nc = s[i+1] if i+1 < n else ''
        if in_s:
            out.append(c)
            if c == '"' and s[i-1] != '\\':
                in_s = False
            elif c == '"' and s[i-1] == '\\' and i-2 >=0 and s[i-2] == '@':
                in_s = False
            i += 1
            continue
        if in_v:
            out.append(c)
            if c == '"' and s[i-1] != '"':
                in_v = False
            i += 1
            continue
        if in_ml:
            if c == '*' and nc == '/':
                in_ml = False
                i += 2
            else:
                i += 1
            continue
        if c == '/' and nc == '/':
            i += 2
            while i < n and s[i] != '\n':
                i += 1
            continue
        if c == '/' and nc == '*':
            in_ml = True
            i += 2
            continue
        if c == '@' and nc == '"':
            in_v = True
            out.append(c)
            i += 1
            continue
        if c == '"':
            in_s = True
            out.append(c)
            i += 1
            continue
        out.append(c)
        i += 1
    return ''.join(out)

def process_file(path):
    with io.open(path, 'r', encoding='utf-8') as f:
        text = f.read()
    new = strip_comments_from_text(text)
    if new != text:
        with io.open(path, 'w', encoding='utf-8') as f:
            f.write(new)

def walk_and_strip(root):
    for dirpath, _, filenames in os.walk(root):
        for name in filenames:
            if name.endswith('.cs'):
                process_file(os.path.join(dirpath, name))

if __name__ == '__main__':
    project_root = os.path.join(os.path.dirname(__file__), '..')
    target = os.path.join(project_root, 'Assets', 'Scripts')
    walk_and_strip(target)

import os
import re

problematic_tests = []
assertion_keywords = [
    'Assert.', '.Should(', '.Throw', '.Verify', '.Be(', 
    '.Contain', '.NotBe', '.HaveCount', 'FluentAssertions',
    'FluentActions', '.BeTrue', '.BeFalse', '.BeNull',
    '.NotBeNull', '.BeEmpty', '.NotBeEmpty', '.Count(',
    '.Any(', '.All(', 'result.IsSuccessful', '.throw('
]

test_patterns = [r'[Tt]ests?\.cs$', r'Test[s]?\.cs$']
exclude_dirs = {'bin', 'obj', '.git'}

total_tests = 0
fake_assert_true = 0
fake_assert_pass = 0
no_assertion_tests = 0

for root, dirs, files in os.walk('.'):
    dirs[:] = [d for d in dirs if d not in exclude_dirs]
    
    for file in files:
        if file.endswith('.cs') and any(re.search(pattern, file) for pattern in test_patterns):
            filepath = os.path.join(root, file)
            
            if 'GlobalUsings' in file or 'Designer' in file or 'AssemblyInfo' in file:
                continue
            
            try:
                with open(filepath, 'r', encoding='utf-8', errors='ignore') as f:
                    content = f.read()
                
                if '[Fact]' not in content and '[Theory]' not in content:
                    continue
                
                test_method_pattern = re.compile(
                    r'\[(?:Fact|Theory)\][^\n]*\n\s*public\s+(?:async\s+)?(?:void|Task[<\w>]*)\s+(\w+)\s*\([^)]*\)\s*\{',
                    re.MULTILINE
                )
                
                for match in test_method_pattern.finditer(content):
                    total_tests += 1
                    method_name = match.group(1)
                    start_pos = match.end()
                    
                    brace_count = 1
                    pos = start_pos
                    method_body_start = pos
                    while pos < len(content) and brace_count > 0:
                        if content[pos] == '{':
                            brace_count += 1
                        elif content[pos] == '}':
                            brace_count -= 1
                        pos += 1
                    
                    method_body = content[method_body_start:pos-1]
                    line_num = content[:match.start()].count('\n') + 1
                    
                    if 'Assert.True(true)' in method_body:
                        fake_assert_true += 1
                        problematic_tests.append({'file': filepath, 'line': line_num, 'method': method_name, 'type': 'Assert.True(true)'})
                    
                    elif 'Assert.Pass()' in method_body:
                        fake_assert_pass += 1
                        problematic_tests.append({'file': filepath, 'line': line_num, 'method': method_name, 'type': 'Assert.Pass()'})
                    
                    else:
                        has_assertion = any(kw in method_body for kw in assertion_keywords)
                        if not has_assertion:
                            no_assertion_tests += 1
                            if no_assertion_tests <= 50:
                                problematic_tests.append({'file': filepath, 'line': line_num, 'method': method_name, 'type': 'NO_ASSERTION'})
                        
            except Exception as e:
                pass

print("FAKE TEST DETECTION REPORT")
print("=" * 70)
print("SUMMARY:")
print(f"  Total Test Methods: {total_tests}")
print(f"  Assert.True(true): {fake_assert_true}")
print(f"  Assert.Pass(): {fake_assert_pass}")
print(f"  No Assertion Keywords: {no_assertion_tests} (showing first 50)")
print()

exact_fakes = fake_assert_true + fake_assert_pass
no_assert_shown = sum(1 for t in problematic_tests if t['type'] == 'NO_ASSERTION')

print("EXACT HILE PATTERNS:")
print(f"  - Assert.True(true): {fake_assert_true} tests")
print(f"  - Assert.Pass(): {fake_assert_pass} tests")
print(f"  - Total exact fake patterns: {exact_fakes}")
print()
print("TESTS WITH NO ASSERTION KEYWORDS:")
print(f"  - Total found: {no_assertion_tests}")
print(f"  - Displaying: {no_assert_shown} (first 50)")
print()

if problematic_tests:
    print("DETAILED LIST (First 50):")
    print("-" * 70)
    for i, test in enumerate(problematic_tests[:50], 1):
        path = test['file'].replace('.\', '').replace('\', '/')
        print(f"{i:3d}. {test['type']:25} | {path}:{test['line']}")
        print(f"     --> {test['method']}")
        
    if len(problematic_tests) > 50:
        print(f"\n... and {len(problematic_tests) - 50} more items")
else:
    print("NO PROBLEMATIC TESTS FOUND")


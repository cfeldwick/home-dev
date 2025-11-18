# Regex Tester - Live Pattern Matching Tool

A sleek, modern browser-based regex testing tool that provides real-time visual feedback as you build and test regular expressions.

## Features

### 🎯 Real-Time Testing
- Instant results as you type your regex pattern or test strings
- No backend required - runs entirely in your browser
- Dynamic table updates with live feedback

### 📊 Smart Table Display
- **Dynamic Columns**: Automatically creates columns for each capture group
- **Named Groups**: Displays capture group names as column headers
- **Numbered Groups**: Falls back to "Group 1", "Group 2", etc. for unnamed groups
- **Multiple Matches**: Shows all matches when using global flag (g)
- **Visual Indicators**: Highlights matches and shows "No match" for failed tests

### 🔍 Regex Analysis
- **Live Validation**: Instantly validates regex syntax
- **Helpful Tooltips**: Shows detailed information about your regex:
  - Number of capture groups
  - Named vs numbered groups
  - Special assertions (lookahead, lookbehind)
  - Syntax error messages
- **Color Coding**: Visual feedback for valid (green) and invalid (red) patterns

### ⚙️ Regex Flags Support
- **g** (global) - Find all matches
- **i** (ignore case) - Case-insensitive matching
- **m** (multiline) - ^ and $ match line boundaries
- **s** (dotAll) - . matches newlines

### 💎 Modern Design
- Gradient purple theme with smooth animations
- Responsive layout works on all screen sizes
- Sticky table headers for easy reference
- Smooth hover effects and transitions
- Clean, intuitive interface

## Usage

### Getting Started

1. **Open the Tool**
   - Simply open `index.html` in any modern web browser
   - No installation or build process required

2. **Enter Your Regex Pattern**
   - Type your regular expression in the "Regular Expression Pattern" field
   - Use named capture groups: `(?<name>pattern)`
   - Or numbered groups: `(pattern)`

3. **Add Test Strings**
   - Enter test strings in the "Test Strings" textarea
   - One string per line
   - Add or remove lines anytime

4. **View Results**
   - Watch the table update in real-time
   - Each row shows one test string
   - Columns show the full match and each capture group
   - Stats show total matches vs total strings

### Example Patterns

#### Email Matcher
```regex
(?<name>\w+)@(?<domain>[\w-]+\.[\w-]+)
```
Test with:
```
john@example.com
jane.doe@company.org
admin@test-site.co.uk
```

#### URL Parser
```regex
(?<protocol>https?):\/\/(?<domain>[\w.-]+)(?<path>\/.*)?
```
Test with:
```
https://example.com/path
http://test.org
https://api.github.com/users
```

#### Date Extractor (MM/DD/YYYY)
```regex
(?<month>\d{2})\/(?<day>\d{2})\/(?<year>\d{4})
```
Test with:
```
12/25/2024
01/01/2025
06/15/2023
```

#### Phone Number
```regex
(?<area>\d{3})-(?<prefix>\d{3})-(?<line>\d{4})
```
Test with:
```
555-123-4567
800-555-0100
123-456-7890
```

## Technical Details

### Browser Compatibility
- Chrome/Edge (latest)
- Firefox (latest)
- Safari (latest)
- Any modern browser with ES6+ support

### Technologies Used
- **HTML5**: Semantic markup
- **CSS3**: Modern styling with gradients, flexbox, and grid
- **Vanilla JavaScript**: No dependencies or frameworks
- **RegExp API**: Native JavaScript regular expressions

### Key Features Implementation

#### Named Capture Groups
The tool automatically detects named capture groups in your pattern:
```javascript
const namedGroupRegex = /\(\?<(\w+)>/g;
```

#### Real-Time Updates
Event listeners on input fields trigger immediate regex execution:
```javascript
regexInput.addEventListener('input', handleRegexChange);
testStringsInput.addEventListener('input', handleTestStringsChange);
```

#### Dynamic Table Generation
Table headers and columns are generated based on the regex structure:
- Extracts group information from the pattern
- Creates columns for each capture group
- Labels columns with group names or numbers

## File Structure

```
regex-tester/
├── index.html          # Complete standalone application
└── README.md          # This file
```

## Use Cases

- **Learning Regex**: Interactive way to learn and practice regular expressions
- **Testing Patterns**: Validate regex patterns before using in code
- **Data Extraction**: Test patterns for parsing logs, emails, URLs, etc.
- **Teaching Tool**: Demonstrate regex concepts with visual feedback
- **Quick Prototyping**: Rapidly test and iterate on regex patterns

## Tips & Tricks

1. **Start Simple**: Begin with a basic pattern and add complexity gradually
2. **Use Named Groups**: Makes results much easier to read and understand
3. **Test Edge Cases**: Add strings that should NOT match to verify your pattern
4. **Global Flag**: Enable 'g' flag to find all matches in each string
5. **Copy Results**: Results are displayed in a table you can copy to spreadsheet apps

## Limitations

- Large datasets (1000+ strings) may impact performance
- No regex replace functionality (test only)
- Limited to JavaScript regex flavor (no PCRE, Python, etc. specific features)
- No persistent storage (reload clears all data)

## Future Enhancements

Potential features for future versions:
- Export results to CSV/JSON
- Import test data from files
- Save/load regex patterns
- Regex cheat sheet reference
- Performance metrics
- Match highlighting in input strings
- Replace functionality
- Regex explanation/breakdown

## Contributing

This is a standalone tool. Feel free to modify and enhance it for your needs!

## License

Free to use and modify for any purpose.

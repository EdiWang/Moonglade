export function calculateReadingTime(blogContent) {
    if (!blogContent || typeof blogContent !== "string") {
        return 0; // Return 0 for empty or invalid input
    }

    // Reading speed constants (words/characters per minute)
    const READING_SPEEDS = {
        ENGLISH_AND_GERMAN: 225,
        CHINESE: 450,
        JAPANESE: 400,
    };

    // Regex patterns for different languages
    const REGEX_PATTERNS = {
        ENGLISH_AND_GERMAN: /\b\w+\b/g, // Matches words
        CHINESE: /[\u4e00-\u9fa5]/g,    // Matches Chinese characters
        JAPANESE: /[\u3040-\u30FF\u31F0-\u31FF\uFF66-\uFF9F\u4E00-\u9FAF]/g, // Matches Japanese characters
    };

    // Helper function to calculate reading time for a given pattern and speed
    const calculateTime = (content, regex, speed) => {
        const matches = content.match(regex) || [];
        return matches.length / speed;
    };

    // Calculate reading times for each language
    const englishAndGermanTime = calculateTime(blogContent, REGEX_PATTERNS.ENGLISH_AND_GERMAN, READING_SPEEDS.ENGLISH_AND_GERMAN);
    const chineseTime = calculateTime(blogContent, REGEX_PATTERNS.CHINESE, READING_SPEEDS.CHINESE);
    const japaneseTime = calculateTime(blogContent, REGEX_PATTERNS.JAPANESE, READING_SPEEDS.JAPANESE);

    // Total reading time in minutes
    const totalReadingTime = englishAndGermanTime + chineseTime + japaneseTime;

    // Round up to the nearest minute
    return Math.ceil(totalReadingTime);
}

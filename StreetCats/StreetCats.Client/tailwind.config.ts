import type { Config } from 'tailwindcss'

export default {
    content: [
        './**/*.{razor,html,cshtml}',
        './**/*.razor.cs'
    ],
    theme: {
        extend: {
            colors: {
                'streetcats-orange': '#f97316',
                'streetcats-green': '#22c55e'
            }
        },
    },
    plugins: [],
} satisfies Config
/** @type {import('tailwindcss').Config} */
export default {
  content: ['./index.html', './src/**/*.{ts,tsx}'],
  theme: {
    extend: {
      colors: {
        pitch: {
          950: '#04101d',
          900: '#071321',
          800: '#0c2033',
          700: '#12314c',
          400: '#32d583',
          300: '#66e6a3',
        },
      },
      boxShadow: {
        glass: '0 25px 60px -30px rgba(0, 0, 0, 0.7)',
      },
      backgroundImage: {
        stadium:
          'radial-gradient(circle at top, rgba(50, 213, 131, 0.18), transparent 30%), linear-gradient(180deg, rgba(4, 16, 29, 0.94), rgba(3, 12, 22, 0.98))',
      },
      fontFamily: {
        display: ['Rajdhani', 'sans-serif'],
        body: ['Space Grotesk', 'sans-serif'],
      },
      keyframes: {
        rise: {
          '0%': { opacity: '0', transform: 'translateY(12px)' },
          '100%': { opacity: '1', transform: 'translateY(0)' },
        },
      },
      animation: {
        rise: 'rise 450ms ease-out both',
      },
    },
  },
  plugins: [],
}

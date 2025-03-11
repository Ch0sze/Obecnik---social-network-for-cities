module.exports = {
  content: [
    "./wwwroot/**/*.html",
    "./wwwroot/js/**/*.js",
    "./Views/**/*.cshtml",
  ],
  theme: {
    extend: {
      fontFamily: {
        inria: ['Inria Sans', 'sans-serif'],
        roboto: ['Roboto', 'sans-serif'],
      },
      colors: {
        blackOpacity: 'rgba(0, 0, 0, 0.12)',
        orange: '#F98866',
      },
      boxShadow: {
        'custom-shadow': '0 4px 6px rgba(0, 0, 0, 0.12)',
      },
    },
  },
  plugins: [],
}

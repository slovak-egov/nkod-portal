var gulp = require('gulp');
var sass = require('gulp-sass')(require('sass'));

gulp.task('sass', function(cb) {
  gulp
    .src('*.scss')
    .pipe(sass({
      errLogToConsole: true,
      includePaths:  ['../node_modules/@id-sk/frontend/idsk']
    }))
    .pipe(
      gulp.dest(function(f) {
        return f.base;
      })
    );
  cb();
});

gulp.task(
  'default',
  gulp.series('sass', function(cb) {
    gulp.watch('*.scss', gulp.series('sass'));
    cb();
  })
);
'use strict';
const EnhancedConflicter = require('./enhanced-conflicter.js');
const through = require('through2');

module.exports = {

  withEnhancedConflicter: function(Generator) {
    return class extends Generator {
      constructor(args, opts) {
        super(args, opts);

        this.conflicter = new EnhancedConflicter(this.conflicter.adapter, this.conflicter.force);
      }

      _updateFileContent(filePath, transformations, options) {
        const { force, ...restOptions } = { force: false, ...options };

        super._updateFileContent(filePath, transformations, restOptions);

        if (force === true) {
          this.conflicter._registerForceUpdate(filePath);
        }
      }
    };
  },

  withBomByFileExtension: function(extensions, Generator) {
    return class extends Generator {
      constructor(args, opts) {
        super(args, opts);

        this.registerTransformStream(
          through.obj(function(file, enc, cb) {
            // The 'vinyl-file' (used by mem-fs for loading files) strips BOM
            // This transform adds BOM to files because e.g. VS saves .csproj files with BOM

            const fileExtension = file.extname.replace('.', '');
            if (extensions.includes(fileExtension)) {
              file.contents = Buffer.concat([new Buffer('\ufeff'), file.contents]);
              return cb(null, file);
            }

            return cb(null, file);
          })
        );
      }
    };
  },

}

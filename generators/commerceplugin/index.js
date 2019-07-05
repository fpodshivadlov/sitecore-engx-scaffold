'use strict';
const chalk = require('chalk');
const uuidv4 = require('uuid/v4');

const BaseGenerator = require('../../lib/base-generator');
const solutionUtils = require('../../lib/solution-utils.js');
const utils = require('../../lib/utils.js');

const msg = require('../../config/messages.json');
const versions = require('../../config/versions.json');
const settings = require('../../config/projectSettings.json');
const baseIgnore = require('../../config/ignore.json');
const commerceVersions = require('../../config/commerce.versions.json');

module.exports = class CommercePluginGenerator extends BaseGenerator {
  constructor(args, opts) {
    super(args, opts);
    
    this.option('solutionName', {
      type: String,
      required: false,
      desc: 'The name of the solution.',
    });
    this.option('sitecoreCommerceVersion', {
      type: String,
      required: false,
      desc: 'The version of Sitecore Commerce to use.',
    });
    this.option('sitecoreVersion', {
      type: String,
      required: false,
      desc: 'The version of sitecore to use.',
    });
    this.option('sitecoreUpdate', {
      type: String,
      required: false,
      desc: 'The version of sitecore to use.',
    });
    this.option('pluginName', {
      type: String,
      required: true,
      desc: 'The name of the module.',
    });

    const config = this.config.getAll();

    if (config && config.promptValues) {
      this.options.solutionName = config.promptValues.solutionName;
      this.options.sitecoreVersion = config.promptValues.sitecoreVersion;
      this.options.sitecoreUpdate = config.promptValues.sitecoreUpdate;
      this.options.sitecoreCommerceVersion = config.promptValues.sitecoreCommerceVersion;
    }

    if (!this.options.sitecoreCommerceVersion) {
      return this._showErrorAndExit(`Sitecore Commerce initial scaffolding required.`);
    }

    this.options.solutionNameUri = config && config.solutionNameUri;
  }

  async prompting() {
    let answers = await this.prompt([
      {
        name: 'solutionName',
        message: msg.solutionName.prompt,
        default: (this.options.solutionName || this.appname),
        when: !this.options.solutionName,
      }, {
        type: 'list',
        name: 'sitecoreVersion',
        message: msg.sitecoreVersion.prompt,
        default: this.options.sitecoreVersion,
        choices: versions,
        when: !this.options.sitecoreVersion,
      },
    ]);

    this.options = { ...this.options, ...answers };

    answers = await this.prompt([
      {
        type: 'list',
        name: 'sitecoreUpdate',
        message: msg.sitecoreUpdate.prompt,
        choices: this.options.sitecoreVersion.value && this.options.sitecoreVersion.value,
        when: !this.options.sitecoreVersion,
      },
    ]);

    this.options = { ...this.options, ...answers };

    const sitecoreVersion = this.options.sitecoreUpdate.exactVersion;
    const compatibleCommerceVersions = commerceVersions.filter(x => x.compatibleWithSitecoreVersion.includes(sitecoreVersion));
    if (compatibleCommerceVersions.length < 1) {
      return this._showErrorAndExit(`No Sitecore Commerce available for Sitecore ${this.options.sitecoreUpdate.exactVersion}.`);
    }

    answers = await this.prompt([
      {
        type: 'list',
        name: 'sitecoreCommerceVersion',
        message: msg.sitecoreCommerceVersion.prompt,
        choices: compatibleCommerceVersions,
        when: !this.options.sitecoreCommerceVersion,
      }, {
        name: 'pluginName',
        message: msg.sitecoreCommercePluginName.prompt,
        when: !this.options.pluginName,
      },
    ]);
    this.options = { ...this.options, ...answers };

    this.options.codeGuidSeed = `${this.options.solutionName}.Plugin.${this.options.pluginName}`;
    this.options.codeGuid = utils.guid(this.options.codeGuidSeed);
    this.options.testGuidSeed = `${this.options.codeGuidSeed}.Tests`;
    this.options.testGuid = utils.guid(this.options.testGuidSeed);
  }

  writing() {
    const destinationPath = this.destinationPath(`src/Commerce/${this.options.solutionName}.Plugin.${this.options.pluginName}`);

    super._runPipeline(this.options.sitecoreUpdate.exactVersion, destinationPath, [
      this._copyAll,
    ]);

    this._addProjectsToSolutionFile();
  }

  /* Copy majority of files with regular template transforms */
  _copyAll(rootPath, destinationPath) {
    super._copyTpl(this.templatePath(`${rootPath}/**/*`), destinationPath, {
        exactVersion: this.options.sitecoreUpdate.exactVersion,
        majorVersion: this.options.sitecoreUpdate.majorVersion,
        netFrameworkVersion: this.options.sitecoreUpdate.netFrameworkVersion,
        kernelVersion: this.options.sitecoreUpdate.kernelVersion,
        solutionX: this.options.solutionName,
        pluginNameX: this.options.pluginName,
        solutionUriX: this.options.solutionNameUri,
      }, {
        ...super._baseGlobOptions(),
        ignore: [...baseIgnore, ...[]]
      }, {
        preProcessPath: this._processPathModuleTokens
      }
    );
  }

  _processPathModuleTokens(destPath) {
    return destPath
      .replace(/SolutionX/g, '<%= solutionX %>')
      .replace(/PluginNameX/g, '<%= pluginNameX %>');
  }

  _replaceTokens(input, options) {
    const content = input instanceof Buffer ? input.toString('utf8') : input;
    return content
      .replace(/(PluginNameX)/g, options.moduleName)
      .replace(/(SolutionX)/g, options.solutionName);
  }

  _addProjectsToSolutionFile() {
    const pluginFolderGuid = uuidv4();
    const destinationPath = this.destinationPath();
    const projectName = `${this.options.solutionName}.Plugin.${this.options.pluginName}`;

    const baseOptions = {
      rootFolderName: "Commerce",
      projectFolderGuid: pluginFolderGuid,
      projectFolderName: projectName,
    };

    super._updateFileContent(`${destinationPath}\\src\\${this.options.solutionName}.Commerce.sln`, [
      c => solutionUtils.addProject(c, {
        ...baseOptions,
        projectName,
        projectPath: `Commerce\\${projectName}\\code\\${projectName}.csproj`,
        projectGuid: this.options.codeGuid,
        projectTypeGuid: settings.coreCodeProject,
      }),
      c => solutionUtils.addProject(c, {
        ...baseOptions,
        projectName: `${projectName}.Tests`,
        projectPath: `Commerce\\${projectName}\\test\\${projectName}.Tests.csproj`,
        projectGuid: this.options.testGuid,
        projectTypeGuid: settings.coreCodeProject,
      }),
    ], {
      force: true
    });
  }

  async end() {
    const projectName = `${this.options.solutionName}.Plugin.${this.options.pluginName}`;

    console.log('');
    console.log('Your commerce engine plugin module ' + chalk.green.bold(projectName)
      + ' has been created and added to ' + chalk.green.bold(this.options.solutionName)
    );
  }
};

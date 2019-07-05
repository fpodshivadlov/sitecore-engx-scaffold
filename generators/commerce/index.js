'use strict';
const chalk = require('chalk');
const uuidv4 = require('uuid/v4');

const BaseGenerator = require('../../lib/base-generator');
const HelixGenerator = require('../app');
const solutionUtils = require('../../lib/solution-utils.js');
const utils = require('../../lib/utils.js');

const baseIgnore = require('../../config/ignore.json');
const msg = require('../../config/messages.json');
const settings = require('../../config/projectSettings.json');
const commerceVersions = require('../../config/commerce.versions.json');
const commerceSettings = require('../../config/commerce.settings.json');

module.exports = class SitecoreCommerceGenerator extends BaseGenerator {
  constructor(args, opts) {
    super(args, opts);

    const instantiate = (Generator, path) => {
      Generator.resolved = require.resolve(path);
      Generator.namespace = this.env.namespace(path);
      return this.env.instantiate(Generator, { opts, arguments: opts.arguments });
    };

    // TODO: investigate for better composability approach
    this.sitecoreGenerator = instantiate(HelixGenerator, "../app");

    this.option('sitecoreCommerceVersion', {
       type: String,
       required: true,
       desc: 'The version of Sitecore Commerce to use.',
       default: commerceVersions[0].value,
    });

    this.options.serviceProxyGuidSeed = "Sitecore.Commerce.ServiceProxy";
    this.options.serviceProxyCodeGuid = utils.guid(this.options.serviceProxyGuidSeed);
  }

  async prompting() {
    await this.sitecoreGenerator.prompting();

    const sitecoreVersion = this.sitecoreGenerator.options.sitecoreUpdate.exactVersion;
    const compatibleCommerceVersions = commerceVersions.filter(x => x.compatibleWithSitecoreVersion.includes(sitecoreVersion));
    if (compatibleCommerceVersions.length < 1) {
      return this._showErrorAndExit(`No Sitecore Commerce available for Sitecore ${this.options.sitecoreUpdate.exactVersion}.`);
    }

    let answers = await this.prompt([{
      type: 'list',
      name: 'sitecoreCommerceVersion',
      message: msg.sitecoreCommerceVersion.prompt,
      choices: compatibleCommerceVersions,
      store: true,
    }]);

    this.options = { ...this.options, ...answers };

    // Adjusting Sitecore properties
    this.sitecoreGenerator.options.vagrantBoxName = (this.options.sitecoreCommerceVersion.value || this.options.sitecoreCommerceVersion).vagrantBoxName;
    this.sitecoreGenerator.options.hostNames = [
      ...this.sitecoreGenerator.options.hostNames || [],
      ...commerceSettings.hostNames
    ];
  }

  writing() {
    this.sitecoreGenerator.writing();

    super._runPipeline(this.options.sitecoreCommerceVersion.exactVersion, this.destinationPath(), [
      this._copyZips,
      this._copyAll
    ]);

    this._addEngineProjectToSolutionFile();
  }

  /* Copy dlls without any transforms */
  _copyZips(rootPath, destinationPath) {
    super._copy(this.templatePath(`${rootPath}/**/*.zip`), destinationPath, {}, super._baseGlobOptions(), {});
  }

  /* Copy majority of files with regular template transforms */
  _copyAll(rootPath, destinationPath) {
    super._copyTpl(this.templatePath(`${rootPath}/**/*`), destinationPath, {
      solutionX: this._getOptionsWithFallback(options => options.solutionName),
      solutionUriX: this._getOptionsWithFallback(options => options.solutionNameUri),
      sitecoreCommerceVersion: this.options.sitecoreCommerceVersion,
      sitecoreCommerceNetFrameworkVersion: this.options.sitecoreCommerceVersion.sitecoreCommerceNetFrameworkVersion,
      exactVersion: this._getOptionsWithFallback(options => options.sitecoreUpdate).exactVersion,
      majorVersion: this._getOptionsWithFallback(options => options.sitecoreUpdate).majorVersion,
      kernelVersion: this._getOptionsWithFallback(options => options.sitecoreUpdate).kernelVersion,
      netFrameworkVersion: this._getOptionsWithFallback(options => options.sitecoreUpdate).netFrameworkVersion,
      vagrantBoxNameX: this._getOptionsWithFallback(options => options.vagrantBoxName),
    }, {
      ...super._baseGlobOptions(),
      ignore: [...baseIgnore, ...[ '**/*.zip' ]]
    }, {
        preProcessPath: this._processPathSolutionToken
    });
  }

  _getOptionsWithFallback(selector) {
    return selector(this.options) || selector(this.sitecoreGenerator.options);
  }

  _processPathSolutionToken(destPath) {
    return destPath.replace(/SolutionX/g, '<%= solutionX %>');
  }

  _addEngineProjectToSolutionFile() {
    const commerceFolderGuid = uuidv4();
    const destinationPath = this.destinationPath();
    const solutionName = this._getOptionsWithFallback(options => options.solutionName);

    super._updateFileContent(`${destinationPath}\\src\\${solutionName}.sln`, [
      c => solutionUtils.addProject(c, {
        rootFolderName: null,
        projectFolderGuid: commerceFolderGuid,
        projectFolderName: "Commerce",
        projectName: "Sitecore.Commerce.ServiceProxy",
        projectPath: commerceSettings.serviceProxyProjectPath,
        projectGuid: this.options.serviceProxyCodeGuid,
        projectTypeGuid: settings.codeProject,
      }),
    ], {
      force: true
    });
  }

  async end() {
    await this.sitecoreGenerator.end();

    const solutionName = this._getOptionsWithFallback(options => options.solutionName);

    console.log('');
    console.log(`The ${chalk.green.bold("Sitecore.Commerce.ServiceProxy")} module has been created and added to ${chalk.green.bold(solutionName)}`);
  }
};

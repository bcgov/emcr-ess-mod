/* tslint:disable */
/* eslint-disable */
import { FeatureFlagConfiguration } from '../models/feature-flag-configuration';
import { OidcConfiguration } from '../models/oidc-configuration';
import { OutageInformation } from '../models/outage-information';
import { TimeoutConfiguration } from '../models/timeout-configuration';
export interface Configuration {
  featureFlags?: FeatureFlagConfiguration;
  oidc?: OidcConfiguration;
  outageInfo?: OutageInformation;
  timeoutInfo?: TimeoutConfiguration;
}

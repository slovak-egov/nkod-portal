import { useTranslation } from 'react-i18next';
import { Dataset, Distribution } from '../client';
import warningIcon from '../icons/warning.png';

type Props = {
    distribution?: Distribution;
    dataset?: Dataset;
};

export default function DataWarningIcon(props: Props) {
    let licenseWarning,
        downloadWarning = false;

    const assertDistribution = function (distribution: Distribution) {
        if (distribution.downloadStatus === false) {
            downloadWarning = true;
        }
        if (!distribution.licenseStatus) {
            licenseWarning = true;
        }
    };

    if (props.distribution) {
        assertDistribution(props.distribution);
    } else if (props.dataset) {
        props.dataset.distributions.forEach(assertDistribution);
    }

    const { t } = useTranslation();
    const warnings = [];
    if (licenseWarning) {
        warnings.push(t('licenseWarning'));
    }
    if (downloadWarning) {
        warnings.push(t('downloadWarning'));
    }

    return (
        <>
            {warnings.length > 0 ? (
                <>
                    <img
                        src={warningIcon}
                        style={{ marginLeft: '5px', width: 'auto', height: '20px', verticalAlign: 'middle' }}
                        alt={warnings.join(', ')}
                        title={warnings.join(', ')}
                    />
                </>
            ) : null}
        </>
    );
}

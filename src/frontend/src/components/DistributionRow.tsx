import { useTranslation } from 'react-i18next';
import { Dataset, Distribution } from '../client';
import FileIcon from './FileIcon';
import { useState } from 'react';

type Props = {
    distribution: Distribution;
    dataset: Dataset;
};

export default function DistributionRow(props: Props) {
    const [expanded, setExpanded] = useState(false);
    const { distribution, dataset } = props;
    const { t } = useTranslation();

    return (
        <div data-testid="distribution">
            <span style={{ display: 'flex', position: 'relative', paddingRight: '30px' }} className={expanded ? 'expanded' : ''}>
                <FileIcon format={distribution.formatValue?.label ?? ''} />
                <span className="govuk-body nkod-detail-distribution-url" style={{ lineHeight: '20px', paddingTop: '20px' }}>
                    {distribution.downloadUrl ? (
                        <a href={distribution.downloadUrl} className="govuk-link" id={'distribution-accordion-heading-' + distribution.id}>
                            {distribution.title && distribution.title.trim().length > 0 ? distribution.title : dataset.name}
                        </a>
                    ) : null}
                </span>
                <span className="govuk-accordion__icon" aria-hidden="true" style={{ cursor: 'pointer' }} onClick={() => setExpanded(!expanded)}></span>
            </span>
            <div
                id={'distribution-accordion-content-' + distribution.id}
                className="govuk-accordion__section-content"
                aria-labelledby={'distribution-accordion-heading-' + distribution.id}
                style={{ display: expanded ? 'block' : 'none', margin: '10px 10px 0 10px' }}
            >
                {distribution.termsOfUse?.authorsWorkTypeValue ? (
                    <p className="govuk-body">
                        {t('authorWorkType')}: {distribution.termsOfUse.authorsWorkTypeValue.label}
                    </p>
                ) : null}

                {distribution.termsOfUse?.originalDatabaseTypeValue ? (
                    <p className="govuk-body">
                        {t('originalDatabaseType')}: {distribution.termsOfUse.originalDatabaseTypeValue.label}
                    </p>
                ) : null}

                {distribution.termsOfUse?.databaseProtectedBySpecialRightsTypeValue ? (
                    <p className="govuk-body">
                        {t('specialDatabaseRights')}: {distribution.termsOfUse.databaseProtectedBySpecialRightsTypeValue.label}
                    </p>
                ) : null}

                {distribution.termsOfUse?.personalDataContainmentTypeValue ? (
                    <p className="govuk-body">
                        {t('personalDataType')}: {distribution.termsOfUse.personalDataContainmentTypeValue.label}
                    </p>
                ) : null}

                {distribution.termsOfUse?.authorName ? (
                    <p className="govuk-body">
                        {t('authorName')}: {distribution.termsOfUse.authorName}
                    </p>
                ) : null}

                {distribution.termsOfUse?.originalDatabaseAuthorName ? (
                    <p className="govuk-body">
                        {t('originalDatabaseAuthorName')}: {distribution.termsOfUse.originalDatabaseAuthorName}
                    </p>
                ) : null}

                {distribution.formatValue ? (
                    <p className="govuk-body">
                        {t('downloadFormat')}: {distribution.formatValue.label}
                    </p>
                ) : null}

                {distribution.mediaTypeValue ? (
                    <p className="govuk-body">
                        {t('mediaType')}: {distribution.mediaTypeValue.label}
                    </p>
                ) : null}

                {distribution.conformsTo ? (
                    <p className="govuk-body">
                        {t('conformsTo')}: {distribution.conformsTo}
                    </p>
                ) : null}

                {distribution.compressFormatValue ? (
                    <p className="govuk-body">
                        {t('compressionMediaType')}: {distribution.compressFormatValue.label}
                    </p>
                ) : null}

                {distribution.packageFormatValue ? (
                    <p className="govuk-body">
                        {t('packageMediaType')}: {distribution.packageFormatValue.label}
                    </p>
                ) : null}
            </div>
            <hr className="govuk-line" aria-hidden="true" />
        </div>
    );
}

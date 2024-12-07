import { useTranslation } from 'react-i18next';
import { CodelistValue, Dataset, Distribution } from '../client';
import FileIcon from './FileIcon';
import { Fragment, useState } from 'react';
import DataWarningIcon from './DataWarningIcon';

type Props = {
    distribution: Distribution;
    dataset: Dataset;
};

function ReferenceLink(props: { value: CodelistValue | null }) {
    if (props.value) {
        return <a href={'https://znalosti.gov.sk/resource?uri=' + encodeURIComponent(props.value.id)}>{props.value.label}</a>;
    }
    return null;
}

export default function DistributionRow(props: Props) {
    const [expanded, setExpanded] = useState(false);
    const { distribution, dataset } = props;
    const { t } = useTranslation();

    return (
        <div data-testid="distribution">
            <span style={{ display: 'flex', position: 'relative', paddingRight: '30px', alignItems: 'center' }} className={expanded ? 'expanded' : ''}>
                <FileIcon format={distribution.formatValue?.label ?? ''} />
                <span className="govuk-body nkod-detail-distribution-url" style={{ lineHeight: '20px', marginBottom: 0, paddingRight: '30px' }}>
                    {distribution.downloadUrl ? (
                        <a href={distribution.downloadUrl} className="govuk-link" id={'distribution-accordion-heading-' + distribution.id}>
                            {distribution.title && distribution.title.trim().length > 0 ? distribution.title : dataset.name}
                        </a>
                    ) : (
                        <span data-testid="service-name">
                            {distribution.title && distribution.title.trim().length > 0 ? distribution.title : dataset.name}{' '}
                            {distribution.isDataService ? <>({t('dataService')})</> : null}
                        </span>
                    )}
                    <DataWarningIcon distribution={distribution} />
                </span>
                <span className="govuk-accordion__icon" aria-hidden="true" style={{ cursor: 'pointer' }} onClick={() => setExpanded(!expanded)}></span>
            </span>
            <div
                id={'distribution-accordion-content-' + distribution.id}
                className="govuk-accordion__section-content"
                aria-labelledby={'distribution-accordion-heading-' + distribution.id}
                style={{ display: expanded ? 'block' : 'none', margin: '10px 10px 0 10px' }}
            >
                {distribution.endpointUrl ? (
                    <p className="govuk-body">
                        {t('endpoint')}: <a href={distribution.endpointUrl}>{distribution.endpointUrl}</a>
                    </p>
                ) : null}

                {distribution.termsOfUse?.authorsWorkTypeValue ? (
                    <p className="govuk-body">
                        {t('authorWorkType')}: <ReferenceLink value={distribution.termsOfUse.authorsWorkTypeValue} />
                    </p>
                ) : null}

                {distribution.termsOfUse?.originalDatabaseTypeValue ? (
                    <p className="govuk-body">
                        {t('originalDatabaseType')}: <ReferenceLink value={distribution.termsOfUse.originalDatabaseTypeValue} />
                    </p>
                ) : null}

                {distribution.termsOfUse?.databaseProtectedBySpecialRightsTypeValue ? (
                    <p className="govuk-body">
                        {t('specialDatabaseRights')}: <ReferenceLink value={distribution.termsOfUse.databaseProtectedBySpecialRightsTypeValue} />
                    </p>
                ) : null}

                {distribution.termsOfUse?.personalDataContainmentTypeValue ? (
                    <p className="govuk-body">
                        {t('personalDataType')}: <ReferenceLink value={distribution.termsOfUse.personalDataContainmentTypeValue} />
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
                        {t('downloadFormat')}: <ReferenceLink value={distribution.formatValue} />
                    </p>
                ) : null}

                {distribution.mediaTypeValue ? (
                    <p className="govuk-body">
                        {t('mediaType')}: <ReferenceLink value={distribution.mediaTypeValue} />
                    </p>
                ) : null}

                {distribution.conformsTo ? (
                    <p className="govuk-body">
                        {t('conformsTo')}: <a href={distribution.conformsTo}>{distribution.conformsTo}</a>
                    </p>
                ) : null}

                {distribution.compressFormatValue ? (
                    <p className="govuk-body">
                        {t('compressionMediaType')}: <ReferenceLink value={distribution.compressFormatValue} />
                    </p>
                ) : null}

                {distribution.packageFormatValue ? (
                    <p className="govuk-body">
                        {t('packageMediaType')}: <ReferenceLink value={distribution.packageFormatValue} />
                    </p>
                ) : null}

                {distribution.hvdCategoryValue ? (
                    <p className="govuk-body">
                        {t('hvdCategory')}: <ReferenceLink value={distribution.hvdCategoryValue} />
                    </p>
                ) : null}

                {distribution.documentation ? (
                    <p className="govuk-body">
                        {t('documentationLink')}: <a href={distribution.documentation}>{distribution.documentation}</a>
                    </p>
                ) : null}

                {distribution.endpointDescription ? (
                    <p className="govuk-body">
                        {t('endpointDescription')}: <a href={distribution.endpointDescription}>{distribution.endpointDescription}</a>
                    </p>
                ) : null}

                {distribution.contactPoint ? (
                    <p className="govuk-body">
                        {t('contactPoint')}: {distribution.contactPoint.name} {distribution.contactPoint.email}
                    </p>
                ) : null}

                {distribution.applicableLegislations.length > 0 ? (
                    <>
                        <p className="govuk-body">
                            {t('applicableLegislations')}:{' '}
                            {distribution.applicableLegislations.map((l) => (
                                <Fragment key={l}>
                                    <div>{l}</div>{' '}
                                </Fragment>
                            ))}
                        </p>
                    </>
                ) : null}
            </div>
            <hr className="govuk-line" aria-hidden="true" />
        </div>
    );
}

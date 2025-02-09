import { useEffect, useMemo, useState } from 'react';
import FormElementGroup from './FormElementGroup';
import MultiRadio from './MultiRadio';
import {
    CodelistValue,
    Dataset,
    DistributionInput,
    extractLanguageErrors,
    knownCodelists,
    useCodelists,
    useDataset,
    useDistributionFileUpload
} from '../client';
import BaseInput from './BaseInput';
import SelectElementItems from './SelectElementItems';
import FileUpload from './FileUpload';
import Alert from './Alert';
import Loading from './Loading';
import ErrorAlert from './ErrorAlert';
import { useTranslation } from 'react-i18next';
import MultiLanguageFormGroup from './MultiLanguageFormGroup';
import MultiTextBox from './MultiTextBox';

type Props = {
    distribution: DistributionInput;
    setDistribution: (properties: Partial<DistributionInput>) => void;
    errors: { [id: string]: string };
    saving: boolean;
    dataset: Dataset | null;
};

const requiredCodelists = [
    knownCodelists.distribution.license,
    knownCodelists.distribution.personalDataContainmentType,
    knownCodelists.distribution.format,
    knownCodelists.distribution.mediaType,
    knownCodelists.dataset.hvdCategory
];

type UploadSetting = {
    name: string;
    id: string;
    enableUpload: boolean;
    enableUrl: boolean;
    enableDataService: boolean;
};

export function DistributionForm(props: Props) {
    const { t } = useTranslation();
    const uploadSettings: UploadSetting[] = useMemo(
        () => [
            {
                name: t('fileIsAvailableOnAddress'),
                id: 'url',
                enableUpload: false,
                enableUrl: true,
                enableDataService: false
            },
            {
                name: t('uploadFileToNkod'),
                id: 'upload',
                enableUpload: true,
                enableUrl: false,
                enableDataService: false
            },
            {
                name: t('fileIsAvailableThroughDataService'),
                id: 'data-service',
                enableUpload: false,
                enableUrl: false,
                enableDataService: true
            }
        ],
        [t]
    );

    const [codelists, loadingCodelists, errorCodelists] = useCodelists(requiredCodelists);
    const [uploadSetting, setUploadSettingState] = useState<UploadSetting>(uploadSettings[0]);
    const [uploading, upload, uploadError] = useDistributionFileUpload();

    const { distribution, setDistribution, errors } = props;

    const licenseCodelist = codelists.find((c) => c.id === knownCodelists.distribution.license);
    const personalDataContainmentTypeCodelist = codelists.find((c) => c.id === knownCodelists.distribution.personalDataContainmentType);
    const formatCodelist = codelists.find((c) => c.id === knownCodelists.distribution.format);
    const mediaTypeCodelist = codelists.find((c) => c.id === knownCodelists.distribution.mediaType);
    const hvdCategoryCodelist = codelists.find((c) => c.id === knownCodelists.dataset.hvdCategory);

    useEffect(() => {
        if (formatCodelist && formatCodelist.values.length > 0 && distribution.format === null) {
            setDistribution({ format: formatCodelist.values[0].id });
        }

        if (mediaTypeCodelist && mediaTypeCodelist.values.length > 0 && distribution.mediaType === null) {
            setDistribution({ mediaType: mediaTypeCodelist.values[0].id });
        }
    }, [formatCodelist, mediaTypeCodelist, distribution, setDistribution]);

    useEffect(() => {
        if (distribution.isDataService && !uploadSetting.enableDataService) {
            setUploadSettingState(uploadSettings[2]);
        }
    }, [distribution, uploadSetting, uploadSettings, setUploadSettingState]);

    const loading = loadingCodelists;
    const error = errorCodelists;
    const saving = props.saving;

    const isHvd = props.dataset?.type.includes('http://publications.europa.eu/resource/authority/dataset-type/HVD') ?? false;

    const distributionTitle = (
        <MultiLanguageFormGroup<string>
            label={t('distributionName')}
            errorMessage={extractLanguageErrors(errors, 'title')}
            values={distribution.title ?? {}}
            onChange={(v) => setDistribution({ title: v })}
            emptyValue=""
            element={(id, value, onChange) => <BaseInput id={id} disabled={saving} value={value} onChange={(e) => onChange(e.target.value)} />}
        />
    );

    const setUploadSetting = (v: UploadSetting) => {
        setUploadSettingState(v);
        setDistribution({ isDataService: v.enableDataService });
    };

    return (
        <>
            {loading ? <Loading /> : null}
            {error ? <ErrorAlert error={error} /> : null}

            {licenseCodelist ? (
                <FormElementGroup
                    label={t('authorWorkType')}
                    errorMessage={errors['authorsworktype']}
                    element={(id) => (
                        <SelectElementItems<CodelistValue>
                            id={id}
                            disabled={saving}
                            options={licenseCodelist.values}
                            selectedValue={distribution.authorsWorkType ?? ''}
                            renderOption={(v) => v.label}
                            getValue={(v) => v.id}
                            onChange={(v) => {
                                setDistribution({ authorsWorkType: v });
                            }}
                        />
                    )}
                />
            ) : null}

            {licenseCodelist ? (
                <FormElementGroup
                    label={t('originalDatabaseType')}
                    errorMessage={errors['originaldatabasetype']}
                    element={(id) => (
                        <SelectElementItems<CodelistValue>
                            id={id}
                            disabled={saving}
                            options={licenseCodelist.values}
                            selectedValue={distribution.originalDatabaseType ?? ''}
                            renderOption={(v) => v.label}
                            getValue={(v) => v.id}
                            onChange={(v) => {
                                setDistribution({ originalDatabaseType: v });
                            }}
                        />
                    )}
                />
            ) : null}

            {licenseCodelist ? (
                <FormElementGroup
                    label={t('specialDatabaseRights')}
                    errorMessage={errors['databaseprotectedbyspecialrightstype']}
                    element={(id) => (
                        <SelectElementItems<CodelistValue>
                            id={id}
                            disabled={saving}
                            options={licenseCodelist.values}
                            selectedValue={distribution.databaseProtectedBySpecialRightsType ?? ''}
                            renderOption={(v) => v.label}
                            getValue={(v) => v.id}
                            onChange={(v) => {
                                setDistribution({ databaseProtectedBySpecialRightsType: v });
                            }}
                        />
                    )}
                />
            ) : null}

            {personalDataContainmentTypeCodelist ? (
                <FormElementGroup
                    label={t('personalDataType')}
                    errorMessage={errors['personaldatacontainmenttype']}
                    element={(id) => (
                        <SelectElementItems<CodelistValue>
                            id={id}
                            disabled={saving}
                            options={personalDataContainmentTypeCodelist.values}
                            selectedValue={distribution.personalDataContainmentType ?? ''}
                            renderOption={(v) => v.label}
                            getValue={(v) => v.id}
                            onChange={(v) => {
                                setDistribution({ personalDataContainmentType: v });
                            }}
                        />
                    )}
                />
            ) : null}

            <FormElementGroup
                label={t('authorName')}
                errorMessage={errors['authorname']}
                element={(id) => (
                    <BaseInput
                        id={id}
                        disabled={saving}
                        value={distribution.authorName ?? ''}
                        onChange={(e) => setDistribution({ authorName: e.target.value })}
                    />
                )}
            />

            <FormElementGroup
                label={t('originalDatabaseAuthorName')}
                errorMessage={errors['originaldatabaseauthorname']}
                element={(id) => (
                    <BaseInput
                        id={id}
                        disabled={saving}
                        value={distribution.originalDatabaseAuthorName ?? ''}
                        onChange={(e) => setDistribution({ originalDatabaseAuthorName: e.target.value })}
                    />
                )}
            />

            <MultiRadio<UploadSetting>
                label={t('distributionFile')}
                options={uploadSettings}
                onChange={setUploadSetting}
                selectedOption={uploadSetting}
                id="upload-settings"
                getValue={(v) => v.id}
                renderOption={(v) => v.name}
            />

            {uploadSetting.enableUrl ? (
                <FormElementGroup
                    label={t('fileDownloadUrl')}
                    errorMessage={errors['downloadurl']}
                    element={(id) => (
                        <BaseInput
                            id={id}
                            disabled={saving}
                            value={distribution.downloadUrl ?? ''}
                            onChange={(e) => setDistribution({ downloadUrl: e.target.value })}
                        />
                    )}
                />
            ) : null}
            {uploadSetting.enableUpload ? (
                <>
                    <FormElementGroup
                        label={t('fileUpload')}
                        errorMessage={errors['downloadurl']}
                        element={(id) => (
                            <FileUpload
                                id={id}
                                disabled={saving}
                                onChange={async (e) => {
                                    const files = e.target.files ?? [];
                                    if (files.length > 0) {
                                        const file = await upload(files[0]);
                                        if (file) {
                                            setDistribution({
                                                downloadUrl: file.url,
                                                fileId: file.id
                                            });
                                        }
                                    }
                                }}
                            />
                        )}
                    />
                    <p className="govuk-hint">{t('maximumFileUploadSize')}: 600 MB</p>

                    {uploading ? <Alert type="info">{t('fileUploadProgress')}</Alert> : null}
                    {uploadError ? <ErrorAlert error={uploadError} /> : null}
                </>
            ) : null}

            <FormElementGroup
                label={t('applicableLegislations')}
                errorMessage={errors['applicablelegislations']}
                element={(id) => (
                    <MultiTextBox
                        id={id}
                        disabled={saving}
                        values={distribution.applicableLegislations}
                        onChange={(e) => setDistribution({ applicableLegislations: e })}
                    />
                )}
            />

            {uploadSetting.enableDataService ? (
                <>
                    {distributionTitle}

                    <FormElementGroup
                        label={t('endpoint')}
                        errorMessage={errors['endpointurl']}
                        element={(id) => (
                            <BaseInput
                                id={id}
                                disabled={saving}
                                value={distribution.endpointUrl ?? ''}
                                onChange={(e) => setDistribution({ endpointUrl: e.target.value })}
                            />
                        )}
                    />

                    <FormElementGroup
                        label={t('documentation')}
                        errorMessage={errors['documentation']}
                        element={(id) => (
                            <BaseInput
                                id={id}
                                disabled={saving}
                                value={distribution.documentation ?? ''}
                                onChange={(e) => setDistribution({ documentation: e.target.value })}
                            />
                        )}
                    />

                    {isHvd && hvdCategoryCodelist ? (
                        <FormElementGroup
                            label={t('hvdCategory')}
                            errorMessage={errors['hvdcategory']}
                            element={(id) => (
                                <SelectElementItems<CodelistValue>
                                    id={id}
                                    disabled={saving}
                                    options={[{ id: '', label: t('none') }, ...hvdCategoryCodelist.values]}
                                    selectedValue={distribution.hvdCategory ?? ''}
                                    renderOption={(v) => v.label}
                                    getValue={(v) => v.id}
                                    onChange={(v) => {
                                        setDistribution({ hvdCategory: v });
                                    }}
                                />
                            )}
                        />
                    ) : null}

                    <FormElementGroup
                        label={t('endpointDescription')}
                        errorMessage={errors['endpointdescription']}
                        element={(id) => (
                            <BaseInput
                                id={id}
                                disabled={saving}
                                value={distribution.endpointDescription ?? ''}
                                onChange={(e) => setDistribution({ endpointDescription: e.target.value })}
                            />
                        )}
                    />

                    <MultiLanguageFormGroup<string>
                        label={t('contactPointName')}
                        values={distribution.contactName}
                        onChange={(v) => setDistribution({ contactName: v })}
                        emptyValue=""
                        errorMessage={extractLanguageErrors(errors, 'contactname')}
                        element={(id, value, onChange) => <BaseInput id={id} disabled={saving} value={value} onChange={(e) => onChange(e.target.value)} />}
                    />
                    <FormElementGroup
                        label={t('contactPointEmail')}
                        errorMessage={errors['contactemail']}
                        element={(id) => (
                            <BaseInput
                                id={id}
                                disabled={saving}
                                value={distribution.contactEmail ?? ''}
                                onChange={(e) => setDistribution({ contactEmail: e.target.value })}
                            />
                        )}
                    />
                </>
            ) : null}

            {formatCodelist ? (
                <FormElementGroup
                    label={t('downloadFormat')}
                    errorMessage={errors['format']}
                    element={(id) => (
                        <SelectElementItems<CodelistValue>
                            id={id}
                            disabled={saving}
                            options={[{ id: '', label: t('none') }, ...formatCodelist.values]}
                            selectedValue={distribution.format ?? ''}
                            renderOption={(v) => v.label}
                            getValue={(v) => v.id}
                            onChange={(v) => {
                                setDistribution({ format: v });
                            }}
                        />
                    )}
                />
            ) : null}

            {mediaTypeCodelist ? (
                <FormElementGroup
                    label={t('mediaType')}
                    errorMessage={errors['mediatype']}
                    element={(id) => (
                        <SelectElementItems<CodelistValue>
                            id={id}
                            disabled={saving}
                            options={mediaTypeCodelist.values}
                            selectedValue={distribution.mediaType ?? ''}
                            renderOption={(v) => v.label}
                            getValue={(v) => v.id}
                            onChange={(v) => {
                                setDistribution({ mediaType: v });
                            }}
                        />
                    )}
                />
            ) : null}

            <FormElementGroup
                label={t('conformsTo')}
                errorMessage={errors['conformsto']}
                element={(id) => (
                    <BaseInput
                        id={id}
                        disabled={saving}
                        value={distribution.conformsTo ?? ''}
                        onChange={(e) => setDistribution({ conformsTo: e.target.value })}
                    />
                )}
            />

            {mediaTypeCodelist ? (
                <FormElementGroup
                    label={t('compressionMediaType')}
                    errorMessage={errors['compressformat']}
                    element={(id) => (
                        <SelectElementItems<CodelistValue>
                            id={id}
                            disabled={saving}
                            options={[{ id: '', label: t('none') }, ...mediaTypeCodelist.values]}
                            selectedValue={distribution.compressFormat ?? ''}
                            renderOption={(v) => v.label}
                            getValue={(v) => v.id}
                            onChange={(v) => {
                                setDistribution({ compressFormat: v === '' ? null : v });
                            }}
                        />
                    )}
                />
            ) : null}

            {mediaTypeCodelist ? (
                <FormElementGroup
                    label={t('packageMediaType')}
                    errorMessage={errors['packageformat']}
                    element={(id) => (
                        <SelectElementItems<CodelistValue>
                            id={id}
                            disabled={saving}
                            options={[{ id: '', label: t('none') }, ...mediaTypeCodelist.values]}
                            selectedValue={distribution.packageFormat ?? ''}
                            renderOption={(v) => v.label}
                            getValue={(v) => v.id}
                            onChange={(v) => {
                                setDistribution({ packageFormat: v === '' ? null : v });
                            }}
                        />
                    )}
                />
            ) : null}

            {!uploadSetting.enableDataService ? <>{distributionTitle}</> : null}
        </>
    );
}

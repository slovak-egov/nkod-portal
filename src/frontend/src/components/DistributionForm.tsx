import { useEffect, useState } from 'react';
import FormElementGroup from './FormElementGroup';
import MultiRadio from './MultiRadio';
import { CodelistValue, DistributionInput, extractLanguageErrors, knownCodelists, useCodelists, useDistributionFileUpload } from '../client';
import BaseInput from './BaseInput';
import SelectElementItems from './SelectElementItems';
import FileUpload from './FileUpload';
import Alert from './Alert';
import Loading from './Loading';
import ErrorAlert from './ErrorAlert';
import { useTranslation } from 'react-i18next';
import MultiLanguageFormGroup from './MultiLanguageFormGroup';

type Props = {
    distribution: DistributionInput;
    setDistribution: (properties: Partial<DistributionInput>) => void;
    errors: { [id: string]: string };
    saving: boolean;
};

const requiredCodelists = [
    knownCodelists.distribution.license,
    knownCodelists.distribution.personalDataContainmentType,
    knownCodelists.distribution.format,
    knownCodelists.distribution.mediaType
];

type UploadSetting = {
    name: string;
    id: string;
    enableUpload: boolean;
    enableUrl: boolean;
};

export function DistributionForm(props: Props) {
    const { t } = useTranslation();
    const uploadSettings: UploadSetting[] = [
        {
            name: t('fileIsAvailableOnAddress'),
            id: 'url',
            enableUpload: false,
            enableUrl: true
        },
        {
            name: t('uploadFileToNkod'),
            id: 'upload',
            enableUpload: true,
            enableUrl: false
        }
    ];

    const [codelists, loadingCodelists, errorCodelists] = useCodelists(requiredCodelists);
    const [uploadSetting, setUploadSetting] = useState<UploadSetting>(uploadSettings[0]);
    const [uploading, upload, uploadError] = useDistributionFileUpload();

    const { distribution, setDistribution, errors } = props;

    const licenseCodelist = codelists.find((c) => c.id === knownCodelists.distribution.license);
    const personalDataContainmentTypeCodelist = codelists.find((c) => c.id === knownCodelists.distribution.personalDataContainmentType);
    const formatCodelist = codelists.find((c) => c.id === knownCodelists.distribution.format);
    const mediaTypeCodelist = codelists.find((c) => c.id === knownCodelists.distribution.mediaType);

    useEffect(() => {
        if (formatCodelist && formatCodelist.values.length > 0 && distribution.format === null) {
            setDistribution({ format: formatCodelist.values[0].id });
        }

        if (mediaTypeCodelist && mediaTypeCodelist.values.length > 0 && distribution.mediaType === null) {
            setDistribution({ mediaType: mediaTypeCodelist.values[0].id });
        }
    }, [formatCodelist, mediaTypeCodelist, distribution, setDistribution]);

    const loading = loadingCodelists;
    const error = errorCodelists;
    const saving = props.saving;

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
            ) : null}
            <p className="govuk-hint">{t('maximumFileUploadSize')}: 30 MB</p>

            {uploading ? <Alert type="info">{t('fileUploadProgress')}</Alert> : null}
            {uploadError ? <ErrorAlert error={uploadError} /> : null}

            {formatCodelist ? (
                <FormElementGroup
                    label={t('downloadFormat')}
                    errorMessage={errors['format']}
                    element={(id) => (
                        <SelectElementItems<CodelistValue>
                            id={id}
                            disabled={saving}
                            options={formatCodelist.values}
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

            <MultiLanguageFormGroup<string>
                label={t('distributionName')}
                errorMessage={extractLanguageErrors(errors, 'title')}
                values={distribution.title ?? {}}
                onChange={(v) => setDistribution({ title: v })}
                emptyValue=""
                element={(id, value, onChange) => <BaseInput id={id} disabled={saving} value={value} onChange={(e) => onChange(e.target.value)} />}
            />
        </>
    );
}

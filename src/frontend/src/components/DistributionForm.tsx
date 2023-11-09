import { useEffect, useState } from "react"
import FormElementGroup from "./FormElementGroup"
import MultiRadio from "./MultiRadio"
import { CodelistValue, DistributionInput, extractLanguageErrors, knownCodelists, supportedLanguages, useCodelists, useDistributionFileUpload } from "../client"
import BaseInput from "./BaseInput"
import SelectElementItems from "./SelectElementItems"
import FileUpload from "./FileUpload"
import Alert from "./Alert"
import Loading from "./Loading"
import ErrorAlert from "./ErrorAlert"
import { useTranslation } from "react-i18next"
import MultiLanguageFormGroup from "./MultiLanguageFormGroup"

type Props = {
    distribution: DistributionInput;
    setDistribution: (properties: Partial<DistributionInput>) => void;
    errors: {[id: string]: string};
    saving: boolean;
}

const requiredCodelists = [
    knownCodelists.distribution.authorsWorkType, 
    knownCodelists.distribution.originalDatabaseType,
    knownCodelists.distribution.databaseProtectedBySpecialRightsType, 
    knownCodelists.distribution.personalDataContainmentType, 
    knownCodelists.distribution.format,
    knownCodelists.distribution.mediaType];

type UploadSetting = {
    name: string;
    id: string;
    enableUpload: boolean;
    enableUrl: boolean;
}
    

export function DistributionForm(props: Props)
{
    const {t} = useTranslation();
    const uploadSettings: UploadSetting[] = [
        {
            name: t('fileIsAvailableOnAddress'),
            id: 'url',
            enableUpload: false,
            enableUrl: true,
        },
        {
            name: t('uploadFileToNkod'),
            id: 'upload',
            enableUpload: true,
            enableUrl: false,
        }
    ];

    const [codelists, loadingCodelists, errorCodelists] = useCodelists(requiredCodelists);
    const [uploadSetting, setUploadSetting] = useState<UploadSetting>(uploadSettings[0]);
    const [ uploading, upload ] = useDistributionFileUpload();

    const { distribution, setDistribution, errors } = props;

    const authorsWorkTypeCodelist = codelists.find(c => c.id === knownCodelists.distribution.authorsWorkType);
    const originalDatabaseTypeCodelist = codelists.find(c => c.id === knownCodelists.distribution.originalDatabaseType);
    const databaseProtectedBySpecialRightsTypeCodelist = codelists.find(c => c.id === knownCodelists.distribution.databaseProtectedBySpecialRightsType);
    const personalDataContainmentTypeCodelist = codelists.find(c => c.id === knownCodelists.distribution.personalDataContainmentType);
    const formatCodelist = codelists.find(c => c.id === knownCodelists.distribution.format);
    const mediaTypeCodelist = codelists.find(c => c.id === knownCodelists.distribution.mediaType);

    useEffect(() => {
        if (formatCodelist && formatCodelist.values.length > 0 && distribution.format === null) {
            setDistribution({format: formatCodelist.values[0].id});
        }

        if (mediaTypeCodelist && mediaTypeCodelist.values.length > 0 && distribution.mediaType === null) {
            setDistribution({mediaType: mediaTypeCodelist.values[0].id});
        }
    }, [formatCodelist, mediaTypeCodelist, distribution, setDistribution]);

    const loading = loadingCodelists;
    const error = errorCodelists;
    const saving = props.saving;

    return <>
        {loading ? <Loading /> : null}
        {error ? <ErrorAlert error={error} /> : null}

        {authorsWorkTypeCodelist ? <FormElementGroup label={t('authorWorkType')} errorMessage={errors['authorsworktype']} element={id => <SelectElementItems<CodelistValue> 
            id={id} 
            disabled={saving}
            options={authorsWorkTypeCodelist.values} 
            selectedValue={distribution.authorsWorkType ?? ''} 
            renderOption={v => v.label} 
            getValue={v => v.id} 
            onChange={v => {setDistribution({authorsWorkType: v}) }} />} /> : null}

        {originalDatabaseTypeCodelist ? <FormElementGroup label={t('originalDatabaseType')} errorMessage={errors['originaldatabasetype']} element={id => <SelectElementItems<CodelistValue> 
            id={id} 
            disabled={saving}
            options={originalDatabaseTypeCodelist.values} 
            selectedValue={distribution.originalDatabaseType ?? ''} 
            renderOption={v => v.label} 
            getValue={v => v.id} 
            onChange={v => {setDistribution({originalDatabaseType: v}) }} />} /> : null}

        {databaseProtectedBySpecialRightsTypeCodelist ? <FormElementGroup label={t('specialDatabaseRights')} errorMessage={errors['databaseprotectedbyspecialrightstype']} element={id => <SelectElementItems<CodelistValue> 
            id={id} 
            disabled={saving}
            options={databaseProtectedBySpecialRightsTypeCodelist.values} 
            selectedValue={distribution.databaseProtectedBySpecialRightsType ?? ''} 
            renderOption={v => v.label} 
            getValue={v => v.id} 
            onChange={v => {setDistribution({databaseProtectedBySpecialRightsType: v}) }} />} /> : null}

        {personalDataContainmentTypeCodelist ? <FormElementGroup label={t('personalDataType')} errorMessage={errors['personaldatacontainmenttype']} element={id => <SelectElementItems<CodelistValue> 
            id={id} 
            disabled={saving}
            options={personalDataContainmentTypeCodelist.values} 
            selectedValue={distribution.personalDataContainmentType ?? ''} 
            renderOption={v => v.label} 
            getValue={v => v.id} 
            onChange={v => {setDistribution({personalDataContainmentType: v}) }} />} /> : null}

        <MultiRadio<UploadSetting> label={t('distributionFile')} options={uploadSettings} onChange={setUploadSetting} selectedOption={uploadSetting} id="upload-settings" getValue={v => v.id} renderOption={v => v.name} />

        {uploadSetting.enableUrl ? <FormElementGroup label={t('fileDownloadUrl')} errorMessage={errors['downloadurl']} element={id => <BaseInput id={id} disabled={saving} value={distribution.downloadUrl ?? ''} onChange={e => setDistribution({downloadUrl: e.target.value})} />} /> : null}
        {uploadSetting.enableUpload ? <FormElementGroup label={t('fileUpload')} errorMessage={errors['downloadurl']} element={id => <FileUpload id={id} disabled={saving} onChange={async e => {
            const files = e.target.files ?? [];
            if (files.length > 0) {
                const file = await upload(files[0]);
                setDistribution({
                    downloadUrl: file.url,
                    fileId: file.id
                });
            }
        }} />} /> : null}

        {uploading ? <Alert type="info">
            Prebieha upload súboru
        </Alert> : null}

        {formatCodelist ? <FormElementGroup label={t('downloadFormat')} errorMessage={errors['format']} element={id => <SelectElementItems<CodelistValue> 
            id={id} 
            disabled={saving}
            options={formatCodelist.values} 
            selectedValue={distribution.format ?? ''} 
            renderOption={v => v.label} 
            getValue={v => v.id} 
            onChange={v => {setDistribution({format: v}) }} />} /> : null}

        {mediaTypeCodelist ? <FormElementGroup label={t('mediaType')} errorMessage={errors['mediatype']} element={id => <SelectElementItems<CodelistValue> 
            id={id} 
            disabled={saving}
            options={mediaTypeCodelist.values} 
            selectedValue={distribution.mediaType ?? ''} 
            renderOption={v => v.label} 
            getValue={v => v.id} 
            onChange={v => {setDistribution({mediaType: v}) }} />} /> : null}

        <FormElementGroup label={t('conformsTo')} errorMessage={errors['conformsto']} element={id => <BaseInput id={id} disabled={saving} value={distribution.conformsTo ?? ''} onChange={e => setDistribution({conformsTo: e.target.value})} />} />

        {mediaTypeCodelist ? <FormElementGroup label={t('compressionMediaType')} errorMessage={errors['compressformat']} element={id => <SelectElementItems<CodelistValue> 
            id={id} 
            disabled={saving}
            options={[{id: '', label: 'nie je'}, ...mediaTypeCodelist.values]} 
            selectedValue={distribution.compressFormat ?? ''} 
            renderOption={v => v.label} 
            getValue={v => v.id}  
            onChange={v => {setDistribution({compressFormat: v === '' ? null : v}) }} />} /> : null}

        {mediaTypeCodelist ? <FormElementGroup label={t('packageMediaType')} errorMessage={errors['packageformat']} element={id => <SelectElementItems<CodelistValue> 
            id={id} 
            disabled={saving}
            options={[{id: '', label: 'nie je'}, ...mediaTypeCodelist.values]} 
            selectedValue={distribution.packageFormat ?? ''} 
            renderOption={v => v.label} 
            getValue={v => v.id} 
            onChange={v => {setDistribution({packageFormat: v === '' ? null : v}) }} />} /> : null}

        <MultiLanguageFormGroup<string> label={t('distributionName')} errorMessage={extractLanguageErrors(errors, 'title')} values={distribution.title ?? {}} onChange={v => setDistribution({title: v})} emptyValue="" element={(id, value, onChange) => <BaseInput id={id} disabled={saving} value={value} onChange={e => onChange(e.target.value)} />} />
        </>
}
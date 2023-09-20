import { useEffect, useState } from "react"
import FormElementGroup from "./FormElementGroup"
import MultiLanguageFormGroup from "./MultiLanguageFormGroup"
import MultiRadio from "./MultiRadio"
import { CodelistValue, DistributionInput, UserInfo, extractLanguageErrors, knownCodelists, supportedLanguages, useCodelists, useDatasets, useSingleFileUpload } from "../client"
import BaseInput from "./BaseInput"
import TextArea from "./TextArea"
import MultiSelectElementItems from "./MultiSelecteElementItems"
import SelectElementItems from "./SelectElementItems"
import CodelistMultiTextBoxAutocomplete from "./CodelistMultiTextBoxAutocomplete"
import MultiTextBox from "./MultiTextBox"
import MultiCheckbox from "./MultiCheckbox"
import FileUpload from "./FileUpload"
import Alert from "./Alert"

type Props = {
    distribution: DistributionInput;
    setDistribution: (properties: Partial<DistributionInput>) => void;
    userInfo: UserInfo|null;
    errors: {[id: string]: string}
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

const uploadSettings: UploadSetting[] = [
    {
        name: 'Súbor je prístupný na adrese',
        id: 'url',
        enableUpload: false,
        enableUrl: true,
    },
    {
        name: 'Nahratie súboru do NKOD',
        id: 'upload',
        enableUpload: true,
        enableUrl: false,
    }
];
    

export function DistributionForm(props: Props)
{
    const [codelists, loadingCodelists, errorCodelists] = useCodelists(requiredCodelists);
    const [uploadSetting, setUploadSetting] = useState<UploadSetting>(uploadSettings[0]);
    const [ uploading, upload ] = useSingleFileUpload();

    const { distribution, setDistribution, userInfo, errors } = props;

    const authorsWorkTypeCodelist = codelists.find(c => c.id === knownCodelists.distribution.authorsWorkType);
    const originalDatabaseTypeCodelist = codelists.find(c => c.id === knownCodelists.distribution.originalDatabaseType);
    const databaseProtectedBySpecialRightsTypeCodelist = codelists.find(c => c.id === knownCodelists.distribution.databaseProtectedBySpecialRightsType);
    const personalDataContainmentTypeCodelist = codelists.find(c => c.id === knownCodelists.distribution.personalDataContainmentType);
    const formatCodelist = codelists.find(c => c.id === knownCodelists.distribution.format);
    const mediaTypeCodelist = codelists.find(c => c.id === knownCodelists.distribution.mediaType);

    return <>
        {authorsWorkTypeCodelist ? <FormElementGroup label="Typ autorského diela" errorMessage={errors['authorsworktype']} element={id => <SelectElementItems<CodelistValue> 
            id={id} 
            options={authorsWorkTypeCodelist.values} 
            selectedValue={distribution.authorsWorkType ?? ''} 
            renderOption={v => v.label} 
            getValue={v => v.id} 
            onChange={v => {setDistribution({authorsWorkType: v}) }} />} /> : null}

        {originalDatabaseTypeCodelist ? <FormElementGroup label="Typ originálnej databázy" errorMessage={errors['originaldatabasetype']} element={id => <SelectElementItems<CodelistValue> 
            id={id} 
            options={originalDatabaseTypeCodelist.values} 
            selectedValue={distribution.originalDatabaseType ?? ''} 
            renderOption={v => v.label} 
            getValue={v => v.id} 
            onChange={v => {setDistribution({originalDatabaseType: v}) }} />} /> : null}

        {databaseProtectedBySpecialRightsTypeCodelist ? <FormElementGroup label="Typ špeciálnej právnej ochrany databázy" errorMessage={errors['databaseprotectedbyspecialrightstype']} element={id => <SelectElementItems<CodelistValue> 
            id={id} 
            options={databaseProtectedBySpecialRightsTypeCodelist.values} 
            selectedValue={distribution.databaseProtectedBySpecialRightsType ?? ''} 
            renderOption={v => v.label} 
            getValue={v => v.id} 
            onChange={v => {setDistribution({databaseProtectedBySpecialRightsType: v}) }} />} /> : null}

        {personalDataContainmentTypeCodelist ? <FormElementGroup label="Typ výskytu osobných údajov" errorMessage={errors['personaldatacontainmenttype']} element={id => <SelectElementItems<CodelistValue> 
            id={id} 
            options={personalDataContainmentTypeCodelist.values} 
            selectedValue={distribution.personalDataContainmentType ?? ''} 
            renderOption={v => v.label} 
            getValue={v => v.id} 
            onChange={v => {setDistribution({personalDataContainmentType: v}) }} />} /> : null}

        <MultiRadio<UploadSetting> label="Súbor distribúcie" options={uploadSettings} onChange={setUploadSetting} selectedOption={uploadSetting} id="upload-settings" getValue={v => v.id} renderOption={v => v.name} />

        {uploadSetting.enableUrl ? <FormElementGroup label="URL súboru na stiahnutie" errorMessage={errors['downloadurl']} element={id => <BaseInput id={id} value={distribution.downloadUrl ?? ''} onChange={e => setDistribution({downloadUrl: e.target.value})} />} /> : null}
        {uploadSetting.enableUpload ? <FormElementGroup label="Upload súboru" errorMessage={errors['downloadurl']} element={id => <FileUpload id={id} onChange={async e => {
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

        {formatCodelist ? <FormElementGroup label="Formát súboru na stiahnutie" errorMessage={errors['format']} element={id => <SelectElementItems<CodelistValue> 
            id={id} 
            options={formatCodelist.values} 
            selectedValue={distribution.format ?? ''} 
            renderOption={v => v.label} 
            getValue={v => v.id} 
            onChange={v => {setDistribution({format: v}) }} />} /> : null}

        {mediaTypeCodelist ? <FormElementGroup label="Typ média súboru na stiahnutie" errorMessage={errors['mediatype']} element={id => <SelectElementItems<CodelistValue> 
            id={id} 
            options={mediaTypeCodelist.values} 
            selectedValue={distribution.mediaType ?? ''} 
            renderOption={v => v.label} 
            getValue={v => v.id} 
            onChange={v => {setDistribution({mediaType: v}) }} />} /> : null}

                </>
}
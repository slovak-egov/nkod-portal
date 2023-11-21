import FormElementGroup from "./FormElementGroup"
import MultiLanguageFormGroup from "./MultiLanguageFormGroup"
import MultiRadio from "./MultiRadio"
import { CodelistValue, LocalCatalogInput, extractLanguageErrors, knownCodelists, useCodelists } from "../client"
import BaseInput from "./BaseInput"
import TextArea from "./TextArea"
import { useTranslation } from "react-i18next"
import Loading from "./Loading"
import ErrorAlert from "./ErrorAlert"

type Props = {
    catalog: LocalCatalogInput;
    setCatalog: (properties: Partial<LocalCatalogInput>) => void;
    errors: {[id: string]: string};
    saving: boolean;
}

type PublicOption = {
    name: string;
    value: boolean;
}

const requiredCodelists = [knownCodelists.catalog.type];

export function LocalCatalogForm(props: Props)
{

    const { catalog, setCatalog, errors } = props;
    const {t} = useTranslation();
    const saving = props.saving;
    const [codelists, loadingCodelists, errorCodelists] = useCodelists(requiredCodelists);

    const publicOptions = [
        {
            name: t('published'),
            value: true
        },
        {
            name: t('notPublished'),
            value: false
        }
    ];

    const loading = loadingCodelists;
    const error = errorCodelists;
    const typeCodelist = codelists.find(c => c.id === knownCodelists.catalog.type);
    

    return <>
        {loading ? <Loading /> : null}
        {error ? <ErrorAlert error={error} /> : null}

        <MultiRadio<PublicOption> label={t('localCatalogState')} 
                                  disabled={saving}
                                    inline 
                                    options={publicOptions} 
                                    id="public-selection" 
                                    getValue={v => v.name} 
                                    renderOption={v => v.name} 
                                    selectedOption={publicOptions.find(o => o.value === catalog.isPublic) ?? publicOptions[0]} 
                                    onChange={o => setCatalog({isPublic: o.value})}  />

        <MultiLanguageFormGroup<string> label={t('catalogName')} errorMessage={extractLanguageErrors(errors, 'name')} values={catalog.name} onChange={v => setCatalog({name: v})} emptyValue="" element={(id, value, onChange) => <BaseInput id={id} disabled={saving} value={value} onChange={e => onChange(e.target.value)} />} />
        <MultiLanguageFormGroup<string> label={t('description')} errorMessage={extractLanguageErrors(errors, 'description')} values={catalog.description} onChange={v => setCatalog({description: v})} emptyValue="" element={(id, value, onChange) => <TextArea id={id} disabled={saving} value={value} onChange={e => onChange(e.target.value)} />} />

        <MultiLanguageFormGroup<string> label={t('contactPointName')} errorMessage={extractLanguageErrors(errors, 'contactname')} values={catalog.contactName} onChange={v => setCatalog({contactName: v})} emptyValue="" element={(id, value, onChange) => <BaseInput id={id} disabled={saving} value={value} onChange={e => onChange(e.target.value)} />} />
        <FormElementGroup label={t('contactPointEmail')} errorMessage={errors['contactemail']} element={id => <BaseInput id={id} disabled={saving} value={catalog.contactEmail ?? ''} onChange={e => setCatalog({contactEmail: e.target.value})} />} />

        <FormElementGroup label={t('catalogHomePage')} errorMessage={errors['homepage']} element={id => <BaseInput id={id} disabled={saving} value={catalog.homePage ?? ''} onChange={e => setCatalog({homePage: e.target.value})} />} />

        {typeCodelist ? <MultiRadio<CodelistValue> label={t('catalogType')} 
                                                  disabled={saving} 
                                                  options={typeCodelist.values} 
                                                  onChange={v => setCatalog({type: v.id})} 
                                                  selectedOption={typeCodelist.values.find(s => s.id === catalog.type) ?? typeCodelist.values[0]} 
                                                  id='catalog-type'
                                                  getValue={v => v.id} 
                                                  renderOption={v => v.label} /> : null}

        <FormElementGroup label={t('catalogEndpoint')} errorMessage={errors['endpointurl']} element={id => <BaseInput id={id} disabled={saving} value={catalog.endpointUrl ?? ''} onChange={e => setCatalog({endpointUrl: e.target.value})} />} />
    </>
}
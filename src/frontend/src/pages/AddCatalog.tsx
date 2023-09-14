import PageHeader from "../components/PageHeader";
import Breadcrumbs from "../components/Breadcrumbs";
import MainContent from "../components/MainContent";
import FormElementGroup from "../components/FormElementGroup";
import BaseInput from "../components/BaseInput";
import Button from "../components/Button";

export default function AddCatalogError()
{
    return <>
            <Breadcrumbs items={[{title: 'Národný katalóg otvorených dát', link: '/'}, {title: 'Zoznam lokálnych katalógov', link: '/'}, {title: 'Nový lokálny katalóg'}]} />
            <MainContent>
                <div className="nkod-form-page">
                    <PageHeader>Nový lokálny katalóg</PageHeader>
                    <p className="govuk-body nkod-publisher-name">
                    <span style={{color: '#2B8CC4', fontWeight: 'bold'}}>Poskytovateľ dát</span><br />
                        Ministerstvo investícií, regionálneho rozvoja a informatizácie Slovenskej republiky 
                    </p>
                    {/* <MultiLanguageFormGroup label="Názov" element={id => <BaseInput id={id} />} /> */}
                    <FormElementGroup label="Meno a priezvisko kontaktnej osoby" element={id => <BaseInput id={id} />} />
                    <FormElementGroup label="E-mail kontaktnej osoby" element={id => <BaseInput id={id} />} />
                    {/* <MultiRadio label="Typ API lokálneho katalógu" items={['DCAT-AP Dokumenty', 'SPARQL Endpoint']} /> */}
                    <FormElementGroup label="URL LKOD API" element={id => <BaseInput id={id} />} />
                    <FormElementGroup label="Domáca stránka lokálneho katalógu" element={id => <BaseInput id={id} />} />
                    <Button>
                        Uložiť
                    </Button>
                </div>
            </MainContent>
        </>;
}
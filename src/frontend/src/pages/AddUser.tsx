import PageHeader from "../components/PageHeader";
import Button from "../components/Button";
import Breadcrumbs from "../components/Breadcrumbs";
import MainContent from "../components/MainContent";
import FormElementGroup from "../components/FormElementGroup";
import BaseInput from "../components/BaseInput";
import { useUserAdd, useUserInfo } from "../client";
import MultiRadio from "../components/MultiRadio";

type Role = {
    id: string;
    name: string;
}

const roles: Role[] = [
    {
        id: 'Publisher',
        name: 'Zverejňovateľ dát'
    },
    {
        id: 'PublisherAdmin',
        name: 'Administrátor zverejňovateľa dát'
    }
]

export default function AddUser()
{
    const [userInfo] = useUserInfo();
    const [user, setUser, errors, saving, saveResult, save] = useUserAdd({
        firstName: '',
        lastName: '',
        email: '',
        role: 'Publisher',
        identificationNumber: '',
    });

    return <>
    <Breadcrumbs items={[{title: 'Národný katalóg otvorených dát', link: '/'}, {title: 'Používatelia', link: '/sprava/pouzivatelia'}, {title: 'Nový používateľ'}]} />
            <MainContent>
            <PageHeader>Nový používateľ</PageHeader>
                {userInfo?.publisherView ? <p className="govuk-body nkod-publisher-name">
                    <span style={{color: '#2B8CC4', fontWeight: 'bold'}}>Poskytovateľ dát</span><br />
                        {userInfo.publisherView.name}
                    </p> : null}

                    <FormElementGroup label="Meno" element={id => <BaseInput id={id} value={user.firstName} onChange={e => setUser({firstName: e.target.value})} />} />
                    <FormElementGroup label="Priezvisko" element={id => <BaseInput id={id} value={user.lastName} onChange={e => setUser({lastName: e.target.value})} />} />
                    <FormElementGroup label="E-mailová adresa" element={id => <BaseInput id={id} value={user.email ?? ''} onChange={e => setUser({email: e.target.value})} />} />
                    <FormElementGroup label="Identifikačné číslo" element={id => <BaseInput id={id} value={user.identificationNumber} onChange={e => setUser({identificationNumber: e.target.value})} />} />

                    <MultiRadio<Role> label="Rola" 
                                    inline 
                                    options={roles} 
                                    id="role-selection" 
                                    getValue={v => v.name} 
                                    renderOption={v => v.name} 
                                    selectedOption={roles.find(o => o.id === user.role) ?? roles[0]} 
                                    onChange={o => setUser({role: o.id})}  />
                    
                    <Button style={{marginRight: '20px'}} onClick={save}>
                        Registrovať 
                    </Button>
            </MainContent>
        </>;
}
import { useTranslation } from "react-i18next";
import Alert from "./Alert";

export default function Loading() {
    const {t} = useTranslation();

    return <Alert type="info">
        <div style={{padding: '5px 10px'}}>
            {t('loading')}
        </div>
    </Alert>;
}
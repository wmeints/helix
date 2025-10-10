import AppConversation from "../organisms/app-conversation";
import AppSidebar from "../organisms/app-sidebar";
import { SidebarProvider, SidebarTrigger } from "../ui/sidebar";

export default function MainLayout() {
    return (
        <SidebarProvider>
            <AppSidebar />
            <main className="flex-1 flex-col relative">
                <div className="absolute left-2 top-2">
                    <SidebarTrigger />
                </div>
                <AppConversation />
            </main>
        </SidebarProvider>
    )
}